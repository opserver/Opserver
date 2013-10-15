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
        private static DateTime _lastFetch = DateTime.UtcNow.AddYears(-1);
        private static List<Build> _builds = new List<Build>();
        private static readonly object _fetchLock = new object();
        private static List<Build> Builds
        {
            get
            {
                if (_lastFetch < DateTime.UtcNow.AddSeconds(-Current.Settings.TeamCity.BuildFetchIntervalSeconds))
                {
                    lock (_fetchLock)
                    {
                        if (_lastFetch < DateTime.UtcNow.AddSeconds(-Current.Settings.TeamCity.BuildFetchIntervalSeconds))
                        {
                            using (MiniProfiler.Current.Step("Get Builds since " + _lastFetch))
                            {
                                var c = GetClient();
                                // grab all new builds with some overlap
                                var newBuilds = c.AllBuildsSinceDate(_lastFetch.AddSeconds(-10));
                                if (newBuilds != null && newBuilds.Any())
                                {
                                    // merge into the list
                                    _builds.AddRange(newBuilds.Where(b => !_builds.Any(eb => eb.Id == b.Id)));
                                    _builds = _builds.OrderBy(b => b.StartDate).ToList();
                                }
                                _lastFetch = DateTime.UtcNow;
                            }
                        }
                    }
                }
                return _builds;
            }
        }

        public static DateTime LastFetch
        {
            get { return _lastFetch; }
        }

        public static bool HasCachePrimed
        {
            get { return _lastFetch >= DateTime.UtcNow.AddSeconds(-Current.Settings.TeamCity.BuildFetchIntervalSeconds); }
        }

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
                        var c = GetClient();
                        result = c.AllBuildConfigs();
                        Current.LocalCache.Set(cacheKey, result, 60*60); // cache for an hour
                    }
                }
                return result;
            }
        }

        public static BuildConfig GetConfig(string buildTypeId)
        {
            var config = Configs.FirstOrDefault(c => c.Id == buildTypeId);
            return config;
        }

        public static List<Build> GetAllBuilds()
        {
            return Builds;
        }

        public static List<Build> GetBuildsByServer(string server)
        {
            List<string> map;
            if (Current.Settings.TeamCity.ServerMaps.TryGetValue(server.ToLowerInvariant(), out map))
            {
                //var builds = csv.Split(StringSplits.Comma_SemiColon).ToList();
                if(map.Any())
                {
                    return map.SelectMany(GetBuildsById).OrderBy(b => b.StartDate).ToList();
                }
            }
            return new List<Build>();
        }

        public static List<Build> GetBuildsById(string buildTypeId)
        {
            return Builds.Where(b => b.BuildTypeId == buildTypeId).ToList();
        }

        public static List<BuildConfig> GetAllBuildConfigs()
        {
            var c = GetClient();
            return c.AllBuildConfigs();
        }

        public static List<BuildConfig> GetBuildConfigsByServer(string server)
        {
            List<string> map;
            if (Current.Settings.TeamCity.ServerMaps.TryGetValue(server, out map))
            {
                //var builds = csv.Split(StringSplits.Comma_SemiColon).ToList();
                if(map.Any())
                {
                    return map.Select(GetConfig).Where(c => c != null).OrderBy(b => b.Project).ThenBy(b => b.Name).ToList();
                }
            }
            return new List<BuildConfig>();
        }

        public static List<Project> GetAllProjects()
        {
            var c = GetClient();
            return c.AllProjects();
        }

        public static TeamCityClient GetClient()
        {
            var client = new TeamCityClient(Current.Settings.TeamCity.Url, useSsl: true);
            client.Connect(Current.Settings.TeamCity.User, Current.Settings.TeamCity.Password);
            return client;
        }
    }
}