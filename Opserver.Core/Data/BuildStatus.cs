using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Profiling;
using TeamCitySharp;
using TeamCitySharp.DomainEntities;

namespace StackExchange.Opserver.Data
{
    public class BuildStatus
    {
        private static List<Build> _builds = new List<Build>();
        private static readonly object _fetchLock = new object();
        private static List<Build> Builds
        {
            get
            {
                if (LastFetch < DateTime.UtcNow.AddSeconds(-Current.Settings.TeamCity.BuildFetchIntervalSeconds))
                {
                    lock (_fetchLock)
                    {
                        if (LastFetch < DateTime.UtcNow.AddSeconds(-Current.Settings.TeamCity.BuildFetchIntervalSeconds))
                        {
                            using (MiniProfiler.Current.Step("Get Builds since " + LastFetch.ToString()))
                            {
                                var c = GetClient();
                                // grab all new builds with some overlap
                                var newBuilds = c.Builds.AllSinceDate(LastFetch.AddSeconds(-10));
                                if (newBuilds != null && newBuilds.Any())
                                {
                                    // merge into the list
                                    _builds.AddRange(newBuilds.Where(b => !_builds.Any(eb => eb.Id == b.Id)));
                                    _builds = _builds.OrderBy(b => b.StartDate).ToList();
                                }
                                LastFetch = DateTime.UtcNow;
                            }
                        }
                    }
                }
                return _builds;
            }
        }

        public static DateTime LastFetch { get; private set; } = DateTime.UtcNow.AddYears(-1);

        public static bool HasCachePrimed =>
            LastFetch >= DateTime.UtcNow.AddSeconds(-Current.Settings.TeamCity.BuildFetchIntervalSeconds);

        public static List<BuildConfig> Configs
        {
            get
            {
                const string cacheKey = "build-configs";
                var result = Current.LocalCache.Get<List<BuildConfig>>(cacheKey);
                if (result == null)
                {
                    using (MiniProfiler.Current.Step("Get Build Configs"))
                    {
                        result = GetAllBuildConfigs();
                        Current.LocalCache.Set(cacheKey, result, 60*60); // cache for an hour
                    }
                }
                return result;
            }
        }

        public static BuildConfig GetConfig(string buildTypeId) => Configs.FirstOrDefault(c => c.Id == buildTypeId);

        public static List<Build> GetAllBuilds() => Builds;

        public static List<Build> GetBuildsByServer(string server)
        {
            List<string> map;
            if (Current.Settings.TeamCity.ServerMaps.TryGetValue(server.ToLowerInvariant(), out map))
            {
                if(map.Any())
                {
                    return map.SelectMany(GetBuildsById).OrderBy(b => b.StartDate).ToList();
                }
            }
            return new List<Build>();
        }

        public static List<Build> GetBuildsById(string buildTypeId) => Builds.Where(b => b.BuildTypeId == buildTypeId).ToList();

        public static List<BuildConfig> GetAllBuildConfigs() => GetClient().BuildConfigs.All();

        public static List<BuildConfig> GetBuildConfigsByServer(string server)
        {
            List<string> map;
            if (Current.Settings.TeamCity.ServerMaps.TryGetValue(server, out map))
            {
                if(map.Any())
                {
                    return map.Select(GetConfig).Where(c => c != null).OrderBy(b => b.Project).ThenBy(b => b.Name).ToList();
                }
            }
            return new List<BuildConfig>();
        }

        public static List<Project> GetAllProjects() => GetClient().Projects.All();

        public static TeamCityClient GetClient()
        {
            Uri uri;
            var client = Uri.TryCreate(Current.Settings.TeamCity.Url, UriKind.Absolute, out uri)
                ? new TeamCityClient(uri.Host + (uri.IsDefaultPort ? "" : ":" + uri.Port.ToString()), useSsl: uri.Scheme == Uri.UriSchemeHttps)
                : new TeamCityClient(Current.Settings.TeamCity.Url, useSsl: false);

            client.Connect(Current.Settings.TeamCity.User, Current.Settings.TeamCity.Password);
            return client;
        }
    }
}