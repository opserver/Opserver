using System;
using System.Collections.Generic;
using System.Net;
using StackExchange.Opserver.Data.Dashboard.Providers;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Interface : IMonitorStatus
    {
        internal DashboardDataProvider DataProvider { get; set; }

        public string Tag { get; internal set; }
        public string Name { get; internal set; }
        public string Description { get; internal set; }
        public string MAC { get; internal set; }
        public long? LinkSpeed { get; internal set; }
        public long? LastInbps { get; internal set; }
        public long? LastOutbps { get; internal set; }
        public long? LastTotalbps { get { return LastInbps + LastOutbps; } }
        
        public List<IPAddress> IPAddresses { get; internal set; }
        

        // TODO: Mean something
        public MonitorStatus MonitorStatus { get { return MonitorStatus.Good; } }
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
                    _prettyName = Description ?? "";
                    foreach (var p in _prettyNameReplacements)
                    {
                        _prettyName = _prettyName.Replace(p.Key, p.Value);
                    }
                }
                return _prettyName;
            }
        }

        private static readonly string[] _speedSizes = {"b", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb"};
        public string PrettySpeed
        {
            get {
                if (!LinkSpeed.HasValue) return "n/a";
                var iSpeed = LinkSpeed.Value;
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
        public Interface(string tag) { Tag = tag; }

        public struct InterfaceUtilization
        {
            public long Epoch { get; internal set; }
            public float Inbps { get; internal set; }
            public float Outbps { get; internal set; }
        }
    }
}