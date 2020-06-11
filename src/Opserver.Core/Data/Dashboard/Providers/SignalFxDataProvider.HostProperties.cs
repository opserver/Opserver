using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
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
            => _hostCache ??= ProviderCache(GetHostPropertiesAsync, 60.Seconds(), 60.Minutes());

        private readonly struct SignalFxHost
        {
            public SignalFxHost(string name, DateTime lastUpdated, ImmutableDictionary<string, string> properties)
            {
                Name = name;
                Properties = properties;
                LastUpdated = lastUpdated;
            }

            public string Name { get; }
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
            public List<SignalFxDimension> Results { get; set; }
        }

        private class SignalFxDimension
        {
            public DateTime LastUpdated { get; set; }
            public string Value { get; set; }
            public Dictionary<string, string> CustomProperties { get; set; }
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
            var url = $"https://api.{Settings.Realm}.signalfx.com/v2/dimension?query=host:*&limit=10000";
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

                    using var s = await wc.OpenReadTaskAsync(url);
                    var result = await JsonSerializer.DeserializeAsync<SignalFxDimensionEnvelope>(s, _deserializerOptions);
                    return result.Results
                        .Where(x => x.CustomProperties.Count > 0)
                        .Select(x => new SignalFxHost(x.Value, x.LastUpdated, x.CustomProperties.ToImmutableDictionary()))
                        .ToImmutableDictionary(x => x.Name);
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
