using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Opserver.Data.MongoDB;

namespace StackExchange.Opserver.Views.MongoDB
{
    public enum MongoDBViews
    {
        All,
        Server,
        Instance
    }
    public class DashboardModel
    {
        public List<MongoDBInstance> Instances { get; set; }
        public string CurrentMongoDBServer { get; set; }
        public MongoDBInstance CurrentInstance { get; set; }
        public bool Refresh { get; set; }
        public MongoDBViews View { get; set; }

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