using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Opserver;
using StackExchange.Exceptional;

namespace Opserver.Data.Dashboard.Providers
{
    public partial class SignalFxDataProvider
    {
        private static readonly ImmutableHashSet<string> _ignoredTags = ImmutableHashSet.Create("dsname", "computationId", "plugin");
        private static readonly SignalFlowStatement[] _signalFlowStatements = new[] {
            new SignalFlowStatement(CpuMetric),
            new SignalFlowStatement(MemoryMetric),
            new SignalFlowStatement(InterfaceRxMetric),
            new SignalFlowStatement(InterfaceTxMetric),
            new SignalFlowStatement(InterfaceRxMetric, "sum(by=['host'])"),
            new SignalFlowStatement(InterfaceTxMetric, "sum(by=['host'])")
        };

        private const string CpuMetric = "cpu.utilization";
        private const string MemoryMetric = "memory.used";
        private const string InterfaceTxMetric = "if_octets.tx";
        private const string InterfaceRxMetric = "if_octets.rx";

        private readonly struct TimeSeries
        {
            public TimeSeries(TimeSeriesKey key, ImmutableArray<GraphPoint> values)
            {
                Key = key;
                Values = values;
            }

            public bool IsDefault => Values.IsDefault;
            public TimeSeriesKey Key { get; }
            public ImmutableArray<GraphPoint> Values { get; }
        }

        private readonly struct TimeSeriesKey : IEquatable<TimeSeriesKey>
        {
            public TimeSeriesKey(string host, string metric) : this(host, metric, ImmutableDictionary<string, string>.Empty)
            {
            }

            public TimeSeriesKey(string host, string metric, ImmutableDictionary<string, string> tags)
            {
                Host = host;
                Metric = metric;
                Tags = tags;
            }

            public string Host { get; }
            public ImmutableDictionary<string, string> Tags { get; }
            public string Metric { get; }

            public bool Equals(TimeSeriesKey other)
            {
                var result = Host == other.Host && Metric == other.Metric;
                if (result)
                {
                    foreach (var tag in Tags)
                    {
                        if (!other.Tags.TryGetValue(tag.Key, out var otherValue) || !other.Tags.ValueComparer.Equals(tag.Value, otherValue))
                        {
                            result &= false;
                            break;
                        }
                    }
                }

                return result;
            }
                
            public override bool Equals(object obj)
            {
                if (!(obj is TimeSeriesKey other))
                {
                    return false;
                }

                return Equals(other);
            }

            public override int GetHashCode()
            {
                var result = Host.GetHashCode() ^ Metric.GetHashCode();
                foreach (var tag in Tags)
                {
                    result ^= tag.Key.GetHashCode() ^ tag.Value.GetHashCode();
                }

                return result;
            }

            public override string ToString() => $"{Host}:{Metric}:{string.Join(",", Tags.Select(x => $"{x.Key}:{x.Value}"))}";
        }

        private Cache<ImmutableDictionary<TimeSeriesKey, TimeSeries>> _dayCache;

        private Cache<ImmutableDictionary<TimeSeriesKey, TimeSeries>> MetricDayCache
            => _dayCache ??= ProviderCache(
                async () =>
                {
                    _logger.LogInformation("Refreshing day cache...");

                    // yearp, we're going and fetching the entire day
                    // every time... we could be smarter here by keeping
                    // a persistent SignalFlowClient around and streaming in
                    // results to the cached data to keep it perpetually upto date
                    // but, for now, simplicity wins. Here we go fetch the 24 hour
                    // window for all the metrics the dashboard cares about in one hit.
                    var sw = Stopwatch.StartNew();
                    var resolution = TimeSpan.FromMinutes(10);
                    var endDate = DateTime.UtcNow.RoundDown(resolution);
                    var startDate = endDate.AddHours(-24);
                    var results = await GetMetricsAsync(_signalFlowStatements, resolution, startDate, endDate);
                    sw.Stop();
                    _logger.LogInformation("Took {0}ms to refresh day cache...", sw.ElapsedMilliseconds);
                    return results.GroupBy(x => x.Key).ToImmutableDictionary(x => x.Key, x => x.First());
                    //return 
                }, 5.Minutes(), 60.Minutes()
            );

        private async Task<ImmutableList<TimeSeries>> GetMetricsAsync(IEnumerable<SignalFlowStatement> metrics, TimeSpan resolution, DateTime start, DateTime? end = null, string host = "*")
        {
            var endDate = end ?? DateTime.UtcNow;
            var startDate = start;
            await using (var signalFlowClient = new SignalFlowClient(Settings.Realm, Settings.AccessToken, _logger))
            {
                await signalFlowClient.ConnectAsync();

                var results = await Task.WhenAll(
                    metrics.Select(m => ExecuteStatementAsync(signalFlowClient, m.ToString(host), resolution, startDate, endDate))
                );

                return results.SelectMany(x => x).ToImmutableList();
            }

            static async Task<ImmutableList<TimeSeries>> ExecuteStatementAsync(SignalFlowClient client, string program, TimeSpan resolution, DateTime startDate, DateTime endDate)
            {
                var timeSeriesMap = new Dictionary<string, TimeSeriesKey>();
                var pointsByHost = new Dictionary<TimeSeriesKey, List<GraphPoint>>();
                await foreach (var msg in client.ExecuteAsync(program, resolution, startDate, endDate))
                {
                    if (msg is MetadataMessage metricMetadata)
                    {
                        // map tags to consistent values
                        var tagBuilder = ImmutableDictionary.CreateBuilder<string, string>();
                        foreach (var tag in metricMetadata.Properties.Dimensions)
                        {
                            if (tag.Key.StartsWith("sf_"))
                            {
                                // ignore SignalFX built-in dimensions
                                continue;
                            }

                            if (_ignoredTags.Contains(tag.Key))
                            {
                                // some tags just aren't interesting
                                continue;
                            }

                            // map some known plugin tags to consistent
                            // tags for things like network interfaces
                            if (tag.Key == "plugin_instance" && metricMetadata.Properties.Dimensions.TryGetValue("plugin", out var plugin) && plugin.ToString().Equals("interface"))
                            {
                                metricMetadata.Properties.Dimensions["interface"] = tag.Value;
                                continue;
                            }

                            var tagValue = tag.Value?.ToString();
                            if (tagValue.HasValue())
                            {
                                tagBuilder[tag.Key] = tagValue;
                            }
                        }

                        timeSeriesMap[metricMetadata.TimeSeriesId] = new TimeSeriesKey(
                            metricMetadata.Properties.Host,
                            metricMetadata.Properties.Metric,
                            tagBuilder.ToImmutable()
                        );
                    }
                    else if (msg is DataMessage metricData)
                    {
                        foreach (var metricValue in metricData.Values)
                        {
                            if (timeSeriesMap.TryGetValue(metricValue.TimeSeriesId, out var timeSeriesKey))
                            {
                                if (!pointsByHost.TryGetValue(timeSeriesKey, out var metricValues))
                                {
                                    pointsByHost[timeSeriesKey] = metricValues = new List<GraphPoint>();
                                }

                                metricValues.Add(
                                    new GraphPoint
                                    {
                                        DateEpoch = metricData.Timestamp.ToEpochTime(),
                                        Value = metricValue.Double
                                    });
                            }
                        }
                    }
                }

                return pointsByHost.Select(
                    x => new TimeSeries(x.Key, x.Value.ToImmutableArray())
                ).ToImmutableList();
            }
        }

        private abstract class SignalFlowMessage
        {
            public abstract string Type { get; }
        }

        private interface IExecutionResult
        {
            string Channel { get; }
        }

        private class ConnectMessage : SignalFlowMessage
        {
            public ConnectMessage(string token) => Token = token;
            public override string Type => "authenticate";
            public string Token { get; }
        }

        private class ExecuteMessage : SignalFlowMessage
        {
            public ExecuteMessage(string channel, string program, TimeSpan resolution, DateTime start, DateTime stop)
            {
                Channel = channel;
                Program = program;
                Start = start;
                Stop = stop;
                Resolution = (int)resolution.TotalMilliseconds;
            }

            public override string Type => "execute";
            public string Channel { get; }
            public string Program { get; }
            public DateTime Start { get; }
            public DateTime Stop { get; }
            public int Resolution { get; }
        }

        private class ControlMessage : SignalFlowMessage, IExecutionResult
        {
            public override string Type => "control-message";
            public string Channel { get; set; }
            public ControlMessageEvent Event { get; set; }
            [JsonPropertyName("timestampMs")]
            public DateTime Timestamp { get; set; }
            public int Progress { get; set; }
        }

        private class MetadataMessage : SignalFlowMessage, IExecutionResult
        {
            public override string Type => "metadata";
            public string Channel { get; set; }
            public MetadataProperties Properties { get; set; }
            [JsonPropertyName("tsId")]
            public string TimeSeriesId { get; set; }
        }

        private class MetadataProperties
        {
            public string Host { get; set; }
            [JsonPropertyName("sf_originatingMetric")]
            public string Metric { get; set; }
            [JsonExtensionData]
            public Dictionary<string, object> Dimensions { get; set; }

        }

        /// <summary>
        /// Explicitly aligned struct that represents a value in a SignalFlow
        /// binary formatted message. This struct overlaps one of a Int32/Int64/Double
        /// value. <see cref="Type"/> discriminates the actual type of the value.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private readonly struct DataMessageValue
        {
            public DataMessageValue(string timeSeriesId, double value, DataMessageValueType type)
            {
                Int32 = 0;
                Int64 = 0;
                Double = value;
                TimeSeriesId = timeSeriesId;
                Type = type;
            }

            public DataMessageValue(string timeSeriesId, uint value, DataMessageValueType type)
            {
                Double = 0;
                Int64 = 0;
                Int32 = value;
                TimeSeriesId = timeSeriesId;
                Type = type;
            }

            public DataMessageValue(string timeSeriesId, ulong value, DataMessageValueType type)
            {
                Double = 0;
                Int32 = 0;
                Int64 = value;
                TimeSeriesId = timeSeriesId;
                Type = type;
            }

            [FieldOffset(0)]
            public readonly string TimeSeriesId;

            [FieldOffset(8)]
            public readonly DataMessageValueType Type;

            [FieldOffset(9)]
            public readonly uint Int32;
            [FieldOffset(9)]
            public readonly ulong Int64;
            [FieldOffset(9)]
            public readonly double Double;
        }

        private enum DataMessageValueType : byte
        {
            Int64 = 1,
            Double = 2,
            Int32 = 3,
        }

        private class DataMessage : SignalFlowMessage, IExecutionResult
        {
            public override string Type => "data";
            public DataMessageFlags Flags { get; private set; }
            public string Channel { get; private set; }
            public DateTime Timestamp { get; private set; }
            public ImmutableArray<DataMessageValue> Values { get; private set; }
            public static DataMessage Parse(ReadOnlySequence<byte> sequence)
            {
                // see https://developers.signalfx.com/signalflow_analytics/rest_api_messages/stream_messages_specification.html#_binary_encoding_of_websocket_messages
                // for documentation of binary payloads
                var reader = new SequenceReader<byte>(sequence);

                // version
                if (!reader.TryRead(out var version) || version != 1)
                {
                    throw new SignalFlowException("Invalid version number when parsing data");
                }

                // message type
                if (!reader.TryRead(out var messageType) || messageType != 5)
                {
                    throw new SignalFlowException("Invalid message type when parsing data");
                }

                // flags
                if (!reader.TryRead(out var flags))
                {
                    throw new SignalFlowException("Invalid message flags when parsing data");
                }

                // reserved byte
                reader.Advance(1);

                // channel
                var channel = reader.GetString(16);

                // timestamp
                if (!reader.TryReadBigEndian(out long logicalTimestamp))
                {
                    throw new SignalFlowException("Invalid timestamp when parsing data");
                }

                // number of items
                if (!reader.TryReadBigEndian(out int count))
                {
                    throw new SignalFlowException("Invalid count when parsing data");
                }

                var values = ImmutableArray.CreateBuilder<DataMessageValue>(count);
                for (var i = 0; i < count; i++)
                {
                    if (!reader.TryRead(out var valueType))
                    {
                        throw new SignalFlowException("Invalid value type when parsing data");
                    }

                    var timeSeriesId = reader.GetBase64EncodedString(8).TrimEnd('=');
                    switch ((DataMessageValueType)valueType)
                    {
                        case DataMessageValueType.Double:
                            {
                                if (!reader.TryReadBigEndian(out double value))
                                {
                                    throw new SignalFlowException("Invalid value when parsing data (double)");
                                }

                                values.Add(
                                    new DataMessageValue(timeSeriesId, value, (DataMessageValueType)valueType)
                                );
                            }
                            break;
                        case DataMessageValueType.Int64:
                            {
                                if (!reader.TryReadBigEndian(out long value))
                                {
                                    throw new SignalFlowException("Invalid value when parsing data (int64)");
                                }

                                values.Add(
                                    new DataMessageValue(timeSeriesId, value, (DataMessageValueType)valueType)
                                );
                            }
                            break;
                        case DataMessageValueType.Int32:
                            {
                                if (!reader.TryReadBigEndian(out int value))
                                {
                                    throw new SignalFlowException("Invalid value when parsing data (int32)");
                                }

                                values.Add(
                                    new DataMessageValue(timeSeriesId, value, (DataMessageValueType)valueType)
                                );
                            }
                            break;
                        default:
                            throw new SignalFlowException("Invalid value when parsing data (type not known)");

                    }
                }

                return new DataMessage
                {
                    Channel = channel,
                    Flags = (DataMessageFlags)flags,
                    Timestamp = logicalTimestamp.FromEpochTime(fromMilliseconds: true),
                    Values = values.MoveToImmutable(),
                };
            }
        }

        [Flags]
        private enum DataMessageFlags
        {
            None = 0,
            Compressed = 0b0000001,
            Json = 0b0000010,
        }

        private enum ControlMessageEvent
        {
            StreamStart,
            JobStart,
            JobProgress,
            ChannelAbort,
            ChannelEnd,
        }

        private class AuthenticatedMessage : SignalFlowMessage
        {
            public override string Type => "authenticated";
        }

        private class SignalFlowException : Exception
        {
            public SignalFlowException(string message) : base(message)
            {
            }

            public SignalFlowException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public SignalFlowException()
            {
            }
        }

        private readonly struct SignalFlowStatement
        {
            public SignalFlowStatement(string metric) : this(metric, null)
            {
            }

            public SignalFlowStatement(string metric, string aggregation)
            {
                Metric = metric;
                Aggregation = aggregation;
            }

            public string Metric { get; }
            public string Aggregation { get; }

            public string ToString(string host)
            {
                var sb = StringBuilderCache.Get();
                sb.Append("data('").Append(Metric).Append("', filter('host', '").Append(host).Append("'))");
                if (Aggregation.HasValue())
                {
                    sb.Append(".").Append(Aggregation);
                }
                sb.Append(".publish();");
                return sb.ToStringRecycle();
            }
        }

        /// <summary>
        /// Implements the SignalFlow protocol over a websocket. It handles the minimal number
        /// of JSON and binary messages needed to process a SignalFlow program and its results.
        ///
        /// It is implemented using the managed <see cref="WebSocket" /> implementation and it reads/writes
        /// protocol messages using <see cref="ChannelReader{T}"/> and <see cref="ChannelWriter{T}"/>.
        ///
        /// The result of a SignalFlow program is exposed as an <see cref="IAsyncEnumerable{T}"/> that a
        /// consumer can enumerate at their own pace.
        /// 
        /// TODOs
        ///  - handle compression of a binary payload 
        /// </summary>
        private class SignalFlowClient : IAsyncDisposable
        {
            private long _channelId;

            private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonEpochConverter()
                }
            };

            private static readonly JsonSerializerOptions _deserializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonEpochConverter(),
                    new JsonControlMessageEventConverter(),
                    new SignalFlowMessageConverter()
                }
            };

            private readonly ILogger _logger;
            private readonly Uri _connectUri;
            private readonly string _accessToken;
            private readonly ClientWebSocket _socket;
            private readonly Channel<SignalFlowMessage> _requestChannel;
            private readonly ConcurrentDictionary<string, Channel<SignalFlowMessage>> _responseChannels = new ConcurrentDictionary<string, Channel<SignalFlowMessage>>();

            private Task _sendTask;
            private Task _receiveTask;

            /// <summary>
            /// Instantiates a new instance of <see cref="SignalFlowClient"/> using
            /// the specified realm (e.g. us1, eu0) and access token.
            /// </summary>
            /// <remarks>
            /// The client will remain unconnected until <see cref="ConnectAsync"/> is called.
            /// </remarks>
            public SignalFlowClient(string realm, string accessToken, ILogger logger)
            {
                _connectUri = new Uri($"wss://stream.{realm}.signalfx.com/v2/signalflow/connect");
                _accessToken = accessToken;
                _socket = new ClientWebSocket();
                _requestChannel = Channel.CreateUnbounded<SignalFlowMessage>();
                _responseChannels[string.Empty] = Channel.CreateUnbounded<SignalFlowMessage>();
                _logger = logger;
            }

            /// <summary>
            /// Connects this client to SignalFx's endpoint.
            /// </summary>
            public async ValueTask ConnectAsync()
            {
                await _socket.ConnectAsync(_connectUri, default);
                await _requestChannel.Writer.WriteAsync(new ConnectMessage(_accessToken));

                // spin up send/receive tasks
                _receiveTask = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await ReadFromSocketAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error while reading froms web socket.");
                            ex.LogNoContext();
                        }
                    });

                _sendTask = Task.Run(
                    async () =>
                    {
                        try
                        {
                            await WriteToSocketAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error while writing to web socket.");
                            ex.LogNoContext();
                        }
                    });

                // wait for the "authenticated" message
                await WaitOneAsync<AuthenticatedMessage>();
            }

            public async IAsyncEnumerable<SignalFlowMessage> ExecuteAsync(string program, TimeSpan resolution, DateTime startDate, DateTime endDate)
            {
                // generate a new channel key
                var channelId = "channel-" + Interlocked.Increment(ref _channelId).ToString();
                var responseChannel = _responseChannels.GetOrAdd(channelId, _ => Channel.CreateUnbounded<SignalFlowMessage>());

                // fire off an execution request
                await _requestChannel.Writer.WriteAsync(
                    new ExecuteMessage(channelId, program, resolution, startDate, endDate)
                );

                // and wait for the responses!
                while (await responseChannel.Reader.WaitToReadAsync())
                {
                    if (responseChannel.Reader.TryRead(out var msg))
                    {
                        if (msg is ControlMessage controlMessage)
                        {
                            if (controlMessage.Event == ControlMessageEvent.ChannelAbort || controlMessage.Event == ControlMessageEvent.ChannelEnd)
                            {
                                // we're done, clean-up
                                responseChannel.Writer.Complete();
                                _responseChannels.TryRemove(channelId, out _);
                                yield break;
                            }
                        }

                        yield return msg;
                    }
                }
            }

            public async ValueTask DisposeAsync()
            {
                if (_socket.State == WebSocketState.Open)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                }

                _requestChannel.Writer.TryComplete();

                await _receiveTask;
                await _sendTask;

                _socket.Dispose();
            }

            private async Task ReadFromSocketAsync()
            {
                while (true)
                {
                    try
                    {
                        var result = await _socket.ReceiveAsync(Memory<byte>.Empty, default);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // end of connection, buh-bye!
                            break;
                        }

                        if (result.EndOfMessage)
                        {
                            // zero-length message, ignore it
                            continue;
                        }

                        var response = await ReadMessageAsync();
                        if (response == null)
                        {
                            // invalid message, ignore it
                            continue;
                        }

                        var channelKey = "";
                        if (response is IExecutionResult executionResult)
                        {
                            channelKey = executionResult.Channel;
                        }

                        if (_responseChannels.TryGetValue(channelKey, out var responseChannel))
                        {
                            await responseChannel.Writer.WriteAsync(response);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error reading from web socket");
                        ex.LogNoContext();
                    }
                }
            }

            private async ValueTask<T> WaitOneAsync<T>() where T : SignalFlowMessage
            {
                if (_responseChannels.TryGetValue(string.Empty, out var responseChannel))
                {
                    while (await responseChannel.Reader.WaitToReadAsync())
                    {
                        if (responseChannel.Reader.TryRead(out var msg) && msg is T typedMsg)
                        {
                            return typedMsg;
                        }
                    }
                }

                return default;
            }

            private async Task<SignalFlowMessage> ReadMessageAsync()
            {
                // borrow a buffer to do the *actual* read
                // if we find we need a bigger buffer then keep returning the buffer and grab a larger one
                var pool = ArrayPool<byte>.Shared;
                var bufferLength = 4096;
                var buffer = pool.Rent(bufferLength);
                var offset = 0;
                var length = 0;
                var resultType = (WebSocketMessageType)0;
                try
                {
                    while (true)
                    {
                        var result = await _socket.ReceiveAsync(buffer.AsMemory(offset), default);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            // see ya, far-side closed the connection
                            return null;
                        }

                        if (result.Count == 0)
                        {
                            // invalid message
                            return null;
                        }

                        resultType = result.MessageType;
                        length += result.Count;
                        offset += result.Count;

                        if (result.EndOfMessage)
                        {
                            // wheee, we got our data!
                            break;
                        }

                        // damn it, we need a bigger buffer!
                        bufferLength *= 2;
                        var newBuffer = pool.Rent(bufferLength);
                        try
                        {
                            Array.Copy(buffer, 0, newBuffer, 0, length);
                        }
                        finally
                        {
                            pool.Return(buffer);
                        }
                        buffer = newBuffer;
                    }

                    // parse the message
                    if (resultType == WebSocketMessageType.Text)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                        {
                            _logger.LogDebug(Encoding.UTF8.GetString(buffer.AsSpan().Slice(0, length)));
                        }

                        // JSON payload, deserialize it
                        return JsonSerializer.Deserialize<SignalFlowMessage>(buffer.AsSpan().Slice(0, length), _deserializerOptions);
                    }
                    else if (resultType == WebSocketMessageType.Binary)
                    {
                        return DataMessage.Parse(new ReadOnlySequence<byte>(buffer, 0, length));
                    }
                }
                finally
                {
                    pool.Return(buffer);
                }

                return null;
            }

            private async Task WriteToSocketAsync()
            {
                var bufferWriter = new ArrayBufferWriter<byte>();
                while (await _requestChannel.Reader.WaitToReadAsync())
                {
                    if (_requestChannel.Reader.TryRead(out var msg))
                    {
                        try
                        {
                            using (var utf8Writer = new Utf8JsonWriter(bufferWriter))
                            {
                                JsonSerializer.Serialize(utf8Writer, msg, msg.GetType(), _serializerOptions);
                            }

                            await _socket.SendAsync(bufferWriter.WrittenMemory, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error writing to web socket");
                            ex.LogNoContext();
                        }
                        finally
                        {
                            bufferWriter.Clear();
                        }
                    }
                }
            }

            private class JsonControlMessageEventConverter : JsonConverter<ControlMessageEvent>
            {
                private static readonly ReadOnlyMemory<byte> _streamStart = Encoding.UTF8.GetBytes("STREAM_START");
                private static readonly ReadOnlyMemory<byte> _jobStart = Encoding.UTF8.GetBytes("JOB_START");
                private static readonly ReadOnlyMemory<byte> _jobProgress = Encoding.UTF8.GetBytes("JOB_PROGRESS");
                private static readonly ReadOnlyMemory<byte> _channelAbort = Encoding.UTF8.GetBytes("CHANNEL_ABORT");
                private static readonly ReadOnlyMemory<byte> _channelEnd = Encoding.UTF8.GetBytes("END_OF_CHANNEL");

                public override ControlMessageEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        throw new JsonException();
                    }

                    if (reader.ValueSpan.SequenceEqual(_streamStart.Span))
                    {
                        return ControlMessageEvent.StreamStart;
                    }

                    if (reader.ValueSpan.SequenceEqual(_jobStart.Span))
                    {
                        return ControlMessageEvent.JobStart;
                    }

                    if (reader.ValueSpan.SequenceEqual(_jobProgress.Span))
                    {
                        return ControlMessageEvent.JobProgress;
                    }

                    if (reader.ValueSpan.SequenceEqual(_channelAbort.Span))
                    {
                        return ControlMessageEvent.ChannelAbort;
                    }

                    if (reader.ValueSpan.SequenceEqual(_channelEnd.Span))
                    {
                        return ControlMessageEvent.ChannelEnd;
                    }

                    throw new JsonException();
                }

                public override void Write(Utf8JsonWriter writer, ControlMessageEvent value, JsonSerializerOptions options) => throw new NotImplementedException();
            }

            private class SignalFlowMessageConverter : JsonConverter<SignalFlowMessage>
            {
                private static readonly ReadOnlyMemory<byte> _typeProperty = Encoding.UTF8.GetBytes("type");

                public override SignalFlowMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    // snapshot the reader at the start of stream
                    // Utf8JsonReader is a mutable struct so capturing in a local
                    // variable here means we can use it in its *current* state later
                    var readerAtStart = reader;

                    if (reader.TokenType != JsonTokenType.StartObject)
                    {
                        throw new JsonException();
                    }

                    string type = null;
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueSpan.SequenceEqual(_typeProperty.Span))
                        {
                            // yay, we have the type property
                            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                            {
                                throw new JsonException();
                            }

                            type = reader.GetString();
                            break;
                        }
                    }

                    // gah, no type; nothing we can do here
                    if (string.IsNullOrEmpty(type))
                    {
                        // nothing we can do here, return a null object
                        return Skip(ref reader);
                    }

                    reader = readerAtStart;
                    return type switch
                    {
                        "control-message" => JsonSerializer.Deserialize<ControlMessage>(ref reader, options),
                        "metadata" => JsonSerializer.Deserialize<MetadataMessage>(ref reader, options),
                        "authenticated" => JsonSerializer.Deserialize<AuthenticatedMessage>(ref reader, options),
                        "message" => Skip(ref reader),
                        _ => throw new JsonException()
                    };


                    static SignalFlowMessage Skip(ref Utf8JsonReader reader)
                    {
                        while (reader.Read()) ;
                        return null; 
                    }
                }

                public override void Write(Utf8JsonWriter writer, SignalFlowMessage value, JsonSerializerOptions options) => throw new NotImplementedException();
            }
        }
    }
}
