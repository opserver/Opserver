using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Exceptional;
using StackExchange.Profiling;

namespace Opserver.Data.Dashboard.Providers
{
    public partial class SignalFxDataProvider
    {
        private Cache<ImmutableDictionary<string, SignalFxHost>> _hostCache;

        private Cache<ImmutableDictionary<string, SignalFxHost>> HostCache
            => _hostCache ??= ProviderCache(GetHostsAsync, 5.Minutes(), 60.Minutes());

        private readonly struct SignalFxHost
        {
            public SignalFxHost(
                string name,
                DateTime lastUpdated,
                ImmutableDictionary<string, string> properties,
                ImmutableArray<string> interfaces,
                ImmutableArray<string> volumes
            )
            {
                Name = name;
                Properties = properties;
                Interfaces = interfaces;
                Volumes = volumes;
                LastUpdated = lastUpdated;
            }

            public string Name { get; }
            public ImmutableArray<string> Interfaces { get; }
            public ImmutableArray<string> Volumes { get; }
            public DateTime LastUpdated { get; }
            public ImmutableDictionary<string, string> Properties { get; }

            public float? GetPropertyAsFloat(string name)
            {
                if (!Properties.TryGetValue(name, out var rawValue) || !float.TryParse(rawValue, out var value))
                {
                    return null;
                }

                return value;
            }

            public int? GetPropertyAsInt32(string name)
            {
                if (!Properties.TryGetValue(name, out var rawValue) || !int.TryParse(rawValue, out var value))
                {
                    return null;
                }

                return value;
            }
        }

        private class SignalFxDimensionEnvelope
        {
            public int Count { get; set; }
            public List<SignalFxDimension> Results { get; set; }
        }

        private class SignalFxDimension
        {
            public DateTime LastUpdated { get; set; }
            public string Value { get; set; }
            public Dictionary<string, string> CustomProperties { get; set; }
            public Dictionary<string, string> Dimensions { get; set; }
        }

        private static readonly JsonSerializerOptions _deserializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
                {
                    new JsonEpochConverter(),
                }
        };

        private async Task<ImmutableDictionary<string, SignalFxHost>> GetHostsAsync()
        {
            var volumeTask = GetVolumesAsync();
            var interfaceTask = GetInterfacesAsync();
            var hostTask = GetHostPropertiesAsync();

            await Task.WhenAll(volumeTask, interfaceTask, hostTask);

            var volumes = volumeTask.Result;
            var interfaces = interfaceTask.Result;
            var hosts = hostTask.Result;
            var results = ImmutableDictionary.CreateBuilder<string, SignalFxHost>(StringComparer.OrdinalIgnoreCase);
            foreach (var host in hosts.OrderByDescending(x => x.Key, StringComparer.Ordinal))
            {
                if (Module.Settings.ExcludePatternRegex?.IsMatch(host.Key) ?? false)
                {
                    continue;
                }

                if (results.ContainsKey(host.Key))
                {
                    continue;
                }

                var hostInterfaces = interfaces.GetValueOrDefault(host.Key, ImmutableArray<string>.Empty);
                var hostVolumes = volumes.GetValueOrDefault(host.Key, ImmutableArray<string>.Empty);
                var properties = host.Value;
                
                results[host.Key] = new SignalFxHost(host.Key, DateTime.UtcNow, properties, hostInterfaces, hostVolumes);
            }

            return results.ToImmutable();
        }

        private async Task<ImmutableDictionary<string, ImmutableDictionary<string, string>>> GetHostPropertiesAsync()
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                var program = $@"data(""{Cpu.Metric}"", filter=filter('host', '*'), extrapolation=""last_value"", maxExtrapolations=2).mean(by=[""host""]).publish()";
                var resolution = TimeSpan.FromSeconds(60);
                var endDate = DateTime.UtcNow.RoundDown(resolution);
                var startDate = endDate.AddMinutes(-15);
                var results = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, string>>();
                await using (var signalFlowClient = new SignalFlowClient(Settings.Realm, Settings.AccessToken, _logger, cts.Token))
                {
                    await signalFlowClient.ConnectAsync();

                    await foreach (var msg in signalFlowClient.ExecuteAsync(program, startDate, endDate, resolution, includeMetadata: true))
                    {
                        if (msg is MetadataMessage metadataMessage)
                        {
                            results[metadataMessage.Properties.Host] = metadataMessage.Properties.Dimensions.ToImmutableDictionary(x => x.Key, x => x.Value.ToString());
                        }
                        else if (msg is DataMessage dataMessage)
                        {
                        }
                    }
                }
                return results.ToImmutable();

            }
        }

        private async Task<ImmutableDictionary<string, ImmutableArray<string>>> GetVolumesAsync()
        {
            var query = "sf_metric:disk.utilization";
            var dimensions = await GetDimensionsAsync("metrictimeseries", query);
            return dimensions
                .Where(x => x.CustomProperties.Count > 0)
                .GroupBy(x => x.CustomProperties["host"], StringComparer.OrdinalIgnoreCase)
                .Select(
                    g =>
                    {
                        var volumes = g.Select(x => x.Dimensions.GetValueOrDefault("device")).Where(x => x.HasValue());
                        var pluginInstances = g.Select(x => x.Dimensions.GetValueOrDefault("plugin_instance")).Where(x => x.HasValue());
                        return (Host: g.Key, Volumes: volumes.Concat(pluginInstances).Distinct());
                    }
                ).ToImmutableDictionary(x => x.Host, x => x.Volumes.ToImmutableArray());
        }

        private async Task<ImmutableDictionary<string, ImmutableArray<string>>> GetInterfacesAsync()
        {
            var query = "interface:* AND sf_metric:if_octets.tx";
            var dimensions = await GetDimensionsAsync("metrictimeseries", query);
            return dimensions
                .Where(x => x.CustomProperties.Count > 0)
                .GroupBy(x => x.CustomProperties["host"], StringComparer.OrdinalIgnoreCase)
                .Select(
                    g =>
                    {
                        var interfaces = g.Select(x => x.Dimensions.GetValueOrDefault("interface")).Where(x => x.HasValue());
                        var pluginInstances = g.Select(x => x.Dimensions.GetValueOrDefault("plugin_instance")).Where(x => x.HasValue());
                        return (Host: g.Key, Interfaces: interfaces.Concat(pluginInstances).Distinct());
                    }
                ).ToImmutableDictionary(x => x.Host, x => x.Interfaces.ToImmutableArray());
        }

        private async Task<ImmutableList<SignalFxDimension>> GetDimensionsAsync(string api, string query)
        {
            var url = $"https://api.{Settings.Realm}.signalfx.com/v2/{api}?query={query.UrlEncode()}&limit=200";
            using (MiniProfiler.Current.Step("SignalFx Host Properties"))
            using (MiniProfiler.Current.CustomTiming("signalFx", url))
            using (var wc = new WebClient())
            {
                try
                {
                    if (Settings.AccessToken.HasValue())
                    {
                        wc.Headers.Add("X-SF-Token", Settings.AccessToken);
                    }

                    var results = ImmutableList.CreateBuilder<SignalFxDimension>();
                    var offset = 0;
                    while (true)
                    {
                        var urlWithOffset = url + "&offset=" + offset.ToString();
                        using var s = await wc.OpenReadTaskAsync(urlWithOffset);
                        var response = await JsonSerializer.DeserializeAsync<SignalFxDimensionEnvelope>(s, _deserializerOptions);
                        results.AddRange(response.Results);
                        offset += response.Results.Count;

                        if (offset >= response.Count)
                        {
                            break;
                        }
                    }

                    return results.ToImmutable();
                }
                catch (Exception e)
                {
                    e.AddLoggedData("Url", url);
                    e.LogNoContext();
                    throw;
                }
            }
        }
    }
}
