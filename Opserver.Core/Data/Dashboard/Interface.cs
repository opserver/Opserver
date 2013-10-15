using System;
using System.Collections.Generic;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Interface : IMonitorStatus
    {
        internal DashboardDataProvider DataProvider { get; set; }

        public int Id { get; internal set; }
        public int NodeId { get; internal set; }
        public int? Index { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public string Name { get; internal set; }
        public string FullName { get; internal set; }
        public string Caption { get; internal set; }
        public string Comments { get; internal set; }
        public string Alias { get; internal set; }
        public string IfName { get; internal set; }
        public string TypeDescription { get; internal set; }
        public string PhysicalAddress { get; internal set; }
        public bool IsTeam { get; internal set; }
        public bool IsUnwatched { get; internal set; }
        public DateTime? UnwatchedFrom { get; internal set; }
        public DateTime? UnwatchedUntil { get; internal set; }

        public NodeStatus Status { get; internal set; }

        public Single? InBps { get; internal set; }
        public Single? OutBps { get; internal set; }
        public Single? InPps { get; internal set; }
        public Single? OutPps { get; internal set; }
        public Single? InPercentUtil { get; internal set; }
        public Single? OutPercentUtil { get; internal set; }
        public int? MTU { get; internal set; }
        public Double? Speed { get; internal set; }

        public MonitorStatus MonitorStatus
        {
            get { return Status.ToMonitorStatus(); }
        }
        // TODO: Implement
        public string MonitorStatusReason { get { return null; } }

        private static readonly Dictionary<string, string> _prettyNameReplacements = new Dictionary<string, string>
            {
                {"Microsoft Network Adapter Multiplexor", "Microsoft NAM"},
                {"Quad Port Server Adapter", "Quad Port SA"},
                {"Microsoft Load Balancing/Failover Provider", "Microsoft LB/FP"},
                {"Microsoft Load Balancing", "Microsoft LB"}
            };

        private string _prettyName;
        public string PrettyName
        {
            get
            {
                if (_prettyName == null)
                {
                    _prettyName = Caption ?? "";
                    foreach (var p in _prettyNameReplacements)
                    {
                        _prettyName = _prettyName.Replace(p.Key, p.Value);
                    }
                }
                return _prettyName;
            }
        }

        private static readonly string[] _speedSizes = new[] {"b", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb"};
        public string PrettySpeed
        {
            get { 
                if (!Speed.HasValue) return "n/a";
                var iSpeed = Speed.Value;
                var order = 0;
                while (iSpeed >= 1000 && order + 1 < _speedSizes.Length)
                {
                    order++;
                    iSpeed = iSpeed/1000;
                }
                return string.Format("{0:0} {1}ps", iSpeed, _speedSizes[order]);
            }
        }

        public Interface() {}
        public Interface(int id) { Id = id; }
    }
}