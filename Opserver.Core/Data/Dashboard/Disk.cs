using System;
using System.Text.RegularExpressions;

namespace StackExchange.Opserver.Data.Dashboard
{
    public class Disk
    {
        // TODO: Not constants eh?
        public const int WarningPercentUsed = 90;
        public const int CriticalPercentUsed = 95;

        public string Name { get; set; }
        public string Description { get; set; }
        public string Tag { get; set; }
        public long? Total { get; set; }
        public long? Used { get; set; }
        public long? Available { get { return Total - Used; } }

        public decimal? PercentUsed
        {
            get { return (Used - Total)/Total; }
        }
        public string SpaceStatusClass
        {
            get
            {
                var pu = PercentUsed;

                if (pu > CriticalPercentUsed)
                    return MonitorStatus.Critical.GetDescription();
                if (pu > WarningPercentUsed)
                    return MonitorStatus.Warning.GetDescription();
                return MonitorStatus.Good.GetDescription();
            }
        }

        private readonly Func<Double?, string> _sizeFormat = s => s.HasValue ? s.Value.ToSize() : "";

        public string PrettyDescription
        {
            get
            {
                var m = Regex.Match(Description, "([a-zA-Z]):\\\\");
                return m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : Description;
            }
        }
        public string PrettyTotal { get { return _sizeFormat(Total); } }
        public string PrettyUsed { get { return _sizeFormat(Used); } }
        public string PrettyAvailable { get { return _sizeFormat(Available); } }

        public class DiskUtilization
        {
            public DateTime DateTime { get; internal set; }
            public long Used { get; internal set; }
            public long Total { get; internal set; }
        }
    }
}
