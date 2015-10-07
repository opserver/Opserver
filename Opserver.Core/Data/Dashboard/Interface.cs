using System;
using System.Collections.Generic;
using System.Net;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Interface : IMonitorStatus
    {
        public string Id { get; internal set; }
        public string NodeId { get; internal set; }
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

        public float? InBps { get; internal set; }
        public float? OutBps { get; internal set; }
        public float? InPps { get; internal set; }
        public float? OutPps { get; internal set; }
        public float? InPercentUtil { get; internal set; }
        public float? OutPercentUtil { get; internal set; }
        public int? MTU { get; internal set; }
        public double? Speed { get; internal set; }

        public List<IPAddress> IPs { get; set; }

        public MonitorStatus MonitorStatus => Status.ToMonitorStatus();
        // TODO: Implement
        public string MonitorStatusReason => null;

        private static readonly Dictionary<string, string> _prettyNameReplacements = new Dictionary<string, string>
            {
                {"Microsoft Network Adapter Multiplexor Driver", "Microsoft Team"},
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
                return $"{iSpeed:0} {_speedSizes[order]}ps";
            }
        }

        public Interface() {}
        public Interface(string id) { Id = id; }
    }
}