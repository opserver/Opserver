using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace StackExchange.Opserver.Data.Exceptions
{
    public class ExceptionsModule : StatusModule<ExceptionsSettings>, IOverallStatusCount
    {
        public override string Name => "Exceptions";
        public override bool Enabled => Stores.Count > 0;

        public List<ExceptionStore> Stores { get; }

        public ExceptionsModule(IOptions<ExceptionsSettings> settings) : base(settings)
        {
            Stores = settings.Value.Stores
                .Select(s => new ExceptionStore(s))
                .Where(s => s.TryAddToGlobalPollers())
                .ToList();
        }

        public override bool IsMember(string node) => false;

        public int TotalExceptionCount =>
            Stores.Sum(s => s.Settings.IncludeInTotal ? s.Applications.Data?.Sum(a => a.ExceptionCount) ?? 0 : 0);
        public int TotalRecentExceptionCount =>
            Stores.Sum(s => s.Settings.IncludeInTotal ? s.Applications.Data?.Sum(a => a.RecentExceptionCount) ?? 0 : 0);

        public override MonitorStatus MonitorStatus
        {
            get
            {
                var snapshot = Settings;
                int total = TotalExceptionCount,
                    recent = TotalRecentExceptionCount;

                if (Stores.Any(s => s.MonitorStatus == MonitorStatus.Critical))
                    return MonitorStatus.Critical;

                if (snapshot.CriticalCount.HasValue && total > snapshot.CriticalCount)
                    return MonitorStatus.Critical;
                if (snapshot.CriticalRecentCount.HasValue && recent > snapshot.CriticalRecentCount)
                    return MonitorStatus.Critical;

                if (snapshot.WarningCount.HasValue && total > snapshot.WarningCount)
                    return MonitorStatus.Warning;
                if (snapshot.WarningRecentCount.HasValue && recent > snapshot.WarningRecentCount)
                    return MonitorStatus.Warning;

                return MonitorStatus.Good;
            }
        }

        int IOverallStatusCount.Count => TotalExceptionCount;
        string IOverallStatusCount.Tooltip => TotalRecentExceptionCount.ToComma() + " recent";

        public ExceptionStore GetStore(string storeName)
        {
            if (storeName.IsNullOrEmpty() || Stores.Count == 1)
            {
                return Stores.FirstOrDefault();
            }
            foreach (var s in Stores)
            {
                if (string.Equals(storeName, s.Name))
                {
                    return s;
                }
            }
            return null;
        }
    }
}
