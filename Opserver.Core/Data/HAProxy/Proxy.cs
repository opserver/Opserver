using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace StackExchange.Opserver.Data.HAProxy
{
    /// <summary>
    /// Represents the a single proxy instance in HAProxy including the Frontends, Servers, and Backends currently configured.
    /// </summary>
    public class Proxy : IMonitorStatus
    {
        [IgnoreDataMember]
        public HAProxyInstance Instance { get; internal set; }
        public string Name { get; internal set; }
        public string GroupName => Instance.Group != null ? Instance.Group.Name : "";
        public string GroupLinkName => string.Concat(Instance.Group != null ? Instance.Group.Name + "-" : "", Name);
        public string InstanceLinkName => string.Concat(Instance.Name, "-", Name);
        public Frontend Frontend { get; internal set; }
        public List<Server> Servers { get; internal set; }
        public Backend Backend { get; internal set; }
        public DateTime PollDate { get; internal set; }

        private List<Item> _allStats; 
        public List<Item> AllStats
        {
            get
            {
                if (_allStats == null)
                {
                    var stats = new List<Item>();
                    if (Frontend != null) stats.Add(Frontend);
                    stats.AddRange(Servers);
                    if (Backend != null) stats.Add(Backend);
                    _allStats = stats;
                }
                return _allStats;
            }
        }

        public bool HasContent => Frontend != null || Backend != null || (Servers != null && Servers.Count > 0);

        public bool HasFrontend => Frontend != null;
        public bool HasServers => Servers != null && Servers.Count > 0;
        public bool HasBackend => Backend != null;

        public MonitorStatus MonitorStatus
        {
            get
            {
                if (Servers == null || Servers.Count == 0)
                    return MonitorStatus.Good;
                return Servers.GetWorstStatus();
            }
        }
        public string MonitorStatusReason
        {
            get
            {
                if (Servers == null || Servers.Count == 0) return null;
                var pieces = new List<string>();
                foreach(var g in Servers.WithIssues().GroupBy(s => s.ProxyServerStatus).OrderByDescending(g => g.Key))
                {
                    pieces.Add($"{NiceName}: {g.Count().ToComma()} {g.Key.ShortDescription()}");
                }
                return string.Join(", ", pieces);
            }
        }

        private Item Primary => (Item)Frontend ?? Backend;

        public string Status => Primary.Status;
        public int LastStatusChangeSecondsAgo => Primary.LastStatusChangeSecondsAgo;
        public long BytesIn => Primary.BytesIn;
        public long BytesOut => Primary.BytesIn;

        public int Order => Frontend != null ? 0 : HasServers ? 1 : 2;

        #region Display Properties

        public bool ShowQueue
        {
            get { return HasServers && Servers.Any(s => s.LimitSessions > 0 || s.LimitNewSessionPerSecond > 0); }
        }
        public bool ShowWarnings => HasServers;

        public bool ShowServers => HasServers;

        public bool ShowThrottle => HasServers && Servers.Any(s => s.Throttle > 0);

        #endregion

        private string _niceName;
        public string NiceName
        {
            get
            {
                if (_niceName != null) return _niceName;
                string result;
                return _niceName = Current.Settings.HAProxy.Aliases.TryGetValue(Name, out result)
                    ? result
                    : Name;
            }
        }

        public override string ToString() =>
            (Instance.Group != null ? Instance.Group.Name + ": " : "") + Instance.Name + ": " + Name;
    }
}