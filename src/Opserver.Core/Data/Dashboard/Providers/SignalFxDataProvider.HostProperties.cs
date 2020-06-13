using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Exceptional;
using StackExchange.Profiling;

namespace Opserver.Data.Dashboard.Providers
{
    public partial class SignalFxDataProvider
    {
        private Cache<ImmutableDictionary<string, SignalFxHost>> _hostCache;

        private Cache<ImmutableDictionary<string, SignalFxHost>> HostCache
            => _hostCache ??= ProviderCache(GetHostPropertiesAsync, 5.Minutes(), 60.Minutes());

        private readonly struct SignalFxHost
        {
            public SignalFxHost(string name, DateTime lastUpdated, ImmutableDictionary<string, string> properties, ImmutableArray<string> interfaces)
            {
                Name = name;
                Properties = properties;
                Interfaces = interfaces;
                LastUpdated = lastUpdated;
            }

            public string Name { get; }
            public ImmutableArray<string> Interfaces { get; }
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

        private async Task<ImmutableDictionary<string, SignalFxHost>> GetHostPropertiesAsync()
        {
            var query = "interface:* AND sf_metric:if_octets.tx";
            var url = $"https://api.{Settings.Realm}.signalfx.com/v2/metrictimeseries?query={query.UrlEncode()}&limit=200";
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

                    var results = new List<SignalFxDimension>();
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

                    return results
                        .Where(x => x.CustomProperties.Count > 0)
                        .GroupBy(x => x.CustomProperties["host"])
                        .Select(
                            g =>
                            {
                                var interfaces = g.Select(x => x.Dimensions.GetValueOrDefault("interface")).Where(x => x != null).ToImmutableArray();
                                var properties = g.SelectMany(x => x.CustomProperties).GroupBy(x => x.Key).ToImmutableDictionary(g => g.Key, g => g.Select(x => x.Value).First());
                                var lastUpdated = g.Max(x => x.LastUpdated);

                                return new SignalFxHost(g.Key, lastUpdated, properties, interfaces);
                            }
                        ).ToImmutableDictionary(x => x.Name);
                }
                catch (Exception e)
                {
                    e.AddLoggedData("Url", url);
                    e.LogNoContext();
                    return ImmutableDictionary<string, SignalFxHost>.Empty;
                }
            }
        }
    }
}
