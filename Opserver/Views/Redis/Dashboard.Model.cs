using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Opserver.Data.Redis;

namespace StackExchange.Opserver.Views.Redis
{
    public enum RedisViews
    {
        All,
        Server,
        Instance
    }
    public class DashboardModel
    {
        public List<RedisInstance> Instances { get; set; }
        public string CurrentRedisServer { get; set; }
        public RedisInstance CurrentInstance { get; set; }
        public bool Refresh { get; set; }
        public RedisViews View { get; set; }

        public bool? _allVersionsMatch;
        public bool AllVersionsMatch
        {
            get { return _allVersionsMatch ?? (_allVersionsMatch = Instances != null && Instances.All(i => i.Version == Instances.First().Version)).Value; }
        }

        public Version CommonVersion
        {
            get { return AllVersionsMatch ? Instances.First().Version : null; }
        }
    }
}