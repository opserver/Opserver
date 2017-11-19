using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Exceptional;

namespace StackExchange.Opserver.Data.Exceptions
{
    public class ExceptionsModule : StatusModule
    {
        public static bool Enabled => Stores.Count > 0;

        public static List<ExceptionStore> Stores { get; }

        static ExceptionsModule()
        {
            Stores = Current.Settings.Exceptions.Stores
                .Select(s => new ExceptionStore(s))
                .Where(s => s.TryAddToGlobalPollers())
                .ToList();
        }

        public override bool IsMember(string node) => false;

        public static int TotalExceptionCount =>
            Stores.Sum(s => s.Settings.IncludeInTotal ? s.Applications.Data?.Sum(a => a.ExceptionCount) ?? 0 : 0);
        public static int TotalRecentExceptionCount =>
            Stores.Sum(s => s.Settings.IncludeInTotal ? s.Applications.Data?.Sum(a => a.RecentExceptionCount) ?? 0 : 0);

        public static MonitorStatus MonitorStatus
        {
            get
            {
                var settings = Current.Settings.Exceptions;
                int total = TotalExceptionCount,
                    recent = TotalRecentExceptionCount;

                if (Stores.Any(s => s.MonitorStatus == MonitorStatus.Critical))
                    return MonitorStatus.Critical;

                if (settings.CriticalCount.HasValue && total > settings.CriticalCount)
                    return MonitorStatus.Critical;
                if (settings.CriticalRecentCount.HasValue && recent > settings.CriticalRecentCount)
                    return MonitorStatus.Critical;

                if (settings.WarningCount.HasValue && total > settings.WarningCount)
                    return MonitorStatus.Warning;
                if (settings.WarningRecentCount.HasValue && recent > settings.WarningRecentCount)
                    return MonitorStatus.Warning;

                return MonitorStatus.Good;
            }
        }

        public static ExceptionStore GetStore(string storeName)
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
