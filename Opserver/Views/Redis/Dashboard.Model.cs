using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Opserver.Data.Redis;

namespace StackExchange.Opserver.Views.Redis
{
    public enum RedisViews
    {
        All = 0,
        Server = 1,
        Instance = 2
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
            get { return _allVersionsMatch ?? (_allVersionsMatch = Instances?.All(i => i.Version == Instances[0].Version) == true).Value; }
        }

        public Version CommonVersion => AllVersionsMatch ? Instances[0].Version : null;

        public List<RedisInstance> Masters { get; private set; }
        public List<RedisInstance> Slaving { get; private set; }
        public List<RedisInstance> Missing { get; private set; }
        public List<RedisInstance> Heads { get; private set; }
        public List<RedisInstance> StandAloneMasters { get; private set; }

        public void Prep()
        {
            Instances = Instances.OrderBy(i => i.Port).ThenBy(i => i.Name).ThenBy(i => i.Host).ToList();
            Masters = Instances.Where(i => i.IsMaster).ToList();
            Slaving = Instances.Where(i => i.IsSlaving).ToList();
            Missing = Instances.Where(i => !Slaving.Contains(i) && (i.Info == null || i.Role == RedisInfo.RedisInstanceRole.Unknown || !i.Info.LastPollSuccessful)).ToList();
            // In the single server view, everything is top level
            Heads = View == RedisViews.Server ? Instances.ToList() : Masters.Where(m => m.SlaveCount > 0).ToList();
            StandAloneMasters = View == RedisViews.Server ? new List<RedisInstance>() : Masters.Where(m => m.SlaveCount == 0 && !Missing.Contains(m)).ToList();
        }
    }
}