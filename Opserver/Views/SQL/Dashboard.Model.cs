using System;
using System.Collections.Generic;
using StackExchange.Opserver.Data.SQL;

namespace StackExchange.Opserver.Views.SQL
{
    public class DashboardModel
    {
        public List<SQLCluster> Clusters { get; set; }
        public List<SQLInstance> StandaloneInstances { get; set; }
        public List<SQLNode.AvailabilityGroupInfo> AvailabilityGroups { get; set; }

        public SQLCluster CurrentCluster { get; set; }
        public string CurrentDatabase { get; set; }
        public int CurrentTable { get; set; }

        public ISearchableNode CurrentServer { get; set; }
        public SQLInstance CurrentInstance { get; set; }
        public SQLInstance.TopSearchOptions TopSearchOptions { get; set; }
        public SQLInstance.ActiveSearchOptions ActiveSearchOptions { get; set; }
        public bool Detailed { get; set; }
        public List<SQLInstance.PerfCounterRecord> PerfCounters { get; set; }

        public int Refresh { get; set; }
        public enum Views
        {
            Servers,
            Instance,
            Active,
            Top,
            Connections,
            Databases
        }
        public Views View { get; set; }

        public enum LastRunInterval
        {
            Week,
            Day,
            Hour,
            FiveMinutes
        }
        public int LastRunIntervalSeconds(LastRunInterval interval)
        {
            switch (interval)
            {
                case LastRunInterval.Week:
                    return 7*24*60*60;
                case LastRunInterval.Day:
                    return 24*60*60;
                case LastRunInterval.Hour:
                    return 60*60;
                case LastRunInterval.FiveMinutes:
                    return 5*60;
                default:
                    throw new ArgumentOutOfRangeException("interval", "WHAT ARE YOU THINKING?!?!?");
            }
        }

        public bool IsInstanceFilterable
        {
            get
            {
                switch (View)
                {
                    case Views.Instance:
                    case Views.Active:
                    case Views.Top:
                    case Views.Connections:
                    case Views.Databases:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}