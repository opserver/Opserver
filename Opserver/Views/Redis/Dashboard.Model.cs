using System;
using System.Collections.Generic;
using StackExchange.Opserver.Data.Redis;

namespace StackExchange.Opserver.Views.Redis
{
    public class DashboardModel
    {
        public List<RedisInstance> Instances { get; set; }
        public string CurrentRedisServer { get; set; }
        public RedisInstance CurrentInstance { get; set; }
        public bool Refresh { get; set; }

        public enum Views
        {
            All,
            Server,
            Instance
        }
        public Views View { get; set; }
    }
}