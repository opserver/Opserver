using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Dashboard
{
    public partial class Interface : IMonitorStatus
    {
        public Node Node { get; set; }

        public string Id { get; internal set; }
        public string NodeId { get; internal set; }
        public DateTime? LastSync { get; internal set; }
        public string Name { get; internal set; }
        public string FullName { get; internal set; }
        public string Caption { get; internal set; }
        public string Comments { get; internal set; }
        public string Alias { get; internal set; }
        public string TypeDescription { get; internal set; }
        public string PhysicalAddress { get; internal set; }
        public bool IsTeam => TeamMembers?.Any() ?? false;
        public bool IsUnwatched { get; internal set; }

        public NodeStatus Status { get; internal set; }

        public float? InBps { get; internal set; }
        public float? OutBps { get; internal set; }
        public float? InPps { get; internal set; }
        public float? OutPps { get; internal set; }
        public int? MTU { get; internal set; }
        public double? Speed { get; internal set; }
        public bool IsTeamMember => Node.Interfaces.Any(i => i.TeamMembers?.Contains(Id) ?? false);
        public List<string> TeamMembers { get; internal set; }
        public List<Interface> TeamMemberInterfaces => Node.Interfaces.Where(i => TeamMembers.Contains(i.Id)).ToList(); 
        public List<IPNet> IPs { get; internal set; }
        public bool DHCPEnabled { get; internal set; }

        public MonitorStatus MonitorStatus => Status.ToMonitorStatus();
        // TODO: Implement
        public string MonitorStatusReason => null;

        private static readonly Dictionary<string, string> _prettyNameReplacements = new Dictionary<string, string>
            {
                {"Microsoft Network Adapter Multiplexor Driver", "Microsoft Team"},
                {"Quad Port Server Adapter", "Quad Port SA"},
                {"Microsoft Load Balancing/Failover Provider", "Microsoft LB/FP"},
                {"Microsoft Load Balancing", "Microsoft LB"},
                {"Intel(R) Ethernet", "Intel" }
            };

        private string _prettyName;
        public string PrettyName
        {
            get
            {
                if (_prettyName == null)
                {
                    _prettyName = Caption ?? Name ?? "";
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
                if (!Speed.HasValue)
                {
                    if (!(TeamMembers?.Any() ?? false))
                    {
                        return "n/a";
                    }
                    Speed = TeamMemberInterfaces?.Sum(i => i.Speed) ?? 0;
                }
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

        public string PrettyMAC =>
            PhysicalAddress?.Length == 12
                ? $"{PhysicalAddress.Substring(0, 2)}-{PhysicalAddress.Substring(2, 2)}-{PhysicalAddress.Substring(4, 2)}-{PhysicalAddress.Substring(6, 2)}-{PhysicalAddress.Substring(8, 2)}-{PhysicalAddress.Substring(10, 2)}"
                : PhysicalAddress;

        internal bool IsLikelyPrimary(Regex pattern) => pattern != null
            ? (FullName != null && pattern.IsMatch(FullName)) ||
              (Name != null && pattern.IsMatch(Name)) ||
              (Caption != null && pattern.IsMatch(Caption))
            : Name.ToLower().EndsWith("team") ||
              Name.ToLower().StartsWith("bond") ||
              Name.Contains("Microsoft Network Adapter Multiplexor Driver");
        
        public Interface() {}
        public Interface(string id) { Id = id; }
    }
}