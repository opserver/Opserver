using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Exceptional;

namespace StackExchange.Opserver.Data.Exceptions
{
    public class ExceptionStores
    {
        public const string AllExceptionsListKey = "Exceptions-GetAllErrors";

        public static List<Application> Applications
        {
            get { return GetApplications(); }
        }

        public static int TotalExceptionCount
        {
            get { return Applications.Sum(a => a.ExceptionCount); }
        }

        public static int TotalRecentExceptionCount
        {
            get { return Applications.Sum(a => a.RecentExceptionCount); }
        }

        public static MonitorStatus MonitorStatus
        {
            get
            {
                var settings = Current.Settings.Exceptions;
                int total = TotalExceptionCount,
                    recent = TotalRecentExceptionCount;

                if (Stores.Any(s => s.MonitorStatus == MonitorStatus.Critical))
                    return MonitorStatus.Critical;

                if (settings.CriticalCount > 0 && total > settings.CriticalCount)
                    return MonitorStatus.Critical;
                if (settings.CriticalRecentCount > 0 && recent > settings.CriticalRecentCount)
                    return MonitorStatus.Critical;

                if (settings.WarningCount > 0 && total > settings.WarningCount)
                    return MonitorStatus.Warning;
                if (settings.WarningRecentCount > 0 && recent > settings.WarningRecentCount)
                    return MonitorStatus.Warning;

                return MonitorStatus.Good;
            }
        }

        public static List<ExceptionStore> Stores { get; private set; }
        public static List<Application> ConfigApplications { get; private set; }

        static ExceptionStores()
        {
            Stores = Current.Settings.Exceptions.Stores
                            .Select(s => new ExceptionStore(s))
                            .Where(s => s.TryAddToGlobalPollers())
                            .ToList();

            ConfigApplications = Current.Settings.Exceptions.Applications
                                        .Select(a => new Application {Name = a}).ToList();
        }

        public static Task<Error> GetError(string appName, Guid guid)
        {
            return GetStores(appName).Select(async s => await s.GetError(guid)).FirstOrDefault(e => e != null);
        }

        public static async Task<T> Action<T>(string appName, Func<ExceptionStore, Task<T>> action)
        {
            var result = default(T);
            foreach(var a in GetApps(appName))
            {
                var aResult = await action(a.Store);
                if (typeof(T) == typeof(bool) && Convert.ToBoolean(aResult))
                {
                    result = aResult;
                }
                if (a.Store != null)
                {
                    a.Store.Applications.Purge();
                    a.Store.ErrorSummary.Purge();
                }
            }
            return result;
        }

        public static IEnumerable<Application> GetApps(string appName)
        {
            return Applications.Where(a => a.Name == appName);
        }

        private static IEnumerable<ExceptionStore> GetStores(string appName)
        {
            var stores = GetApps(appName).Where(a => a.Store != null).Select(a => a.Store).Distinct();
            // if we have no error stores hooked up, return all stores to search
            return stores.Any() ? stores : Applications.Where(a => a.Store != null).Select(a => a.Store).Distinct();
        }

        private static IEnumerable<Error> GetSorted(IEnumerable<Error> source, ExceptionSorts? sort = ExceptionSorts.TimeDesc)
        {
            switch (sort)
            {
                case ExceptionSorts.AppAsc:
                    return source.OrderBy(e => e.ApplicationName);
                case ExceptionSorts.AppDesc:
                    return source.OrderByDescending(e => e.ApplicationName);
                case ExceptionSorts.TypeAsc:
                    return source.OrderBy(e => e.Type.Split(StringSplits.Period_Plus).Last());
                case ExceptionSorts.TypeDesc:
                    return source.OrderByDescending(e => e.Type.Split(StringSplits.Period_Plus).Last());
                case ExceptionSorts.MessageAsc:
                    return source.OrderBy(e => e.Message);
                case ExceptionSorts.MessageDesc:
                    return source.OrderByDescending(e => e.Message);
                case ExceptionSorts.UrlAsc:
                    return source.OrderBy(e => e.Url);
                case ExceptionSorts.UrlDesc:
                    return source.OrderByDescending(e => e.Url);
                case ExceptionSorts.IPAddressAsc:
                    return source.OrderBy(e => e.IPAddress);
                case ExceptionSorts.IPAddressDesc:
                    return source.OrderByDescending(e => e.IPAddress);
                case ExceptionSorts.HostAsc:
                    return source.OrderBy(e => e.Host);
                case ExceptionSorts.HostDesc:
                    return source.OrderByDescending(e => e.Host);
                case ExceptionSorts.MachineNameAsc:
                    return source.OrderBy(e => e.MachineName);
                case ExceptionSorts.MachineNameDesc:
                    return source.OrderByDescending(e => e.MachineName);
                case ExceptionSorts.CountAsc:
                    return source.OrderBy(e => e.DuplicateCount.GetValueOrDefault(1));
                case ExceptionSorts.CountDesc:
                    return source.OrderByDescending(e => e.DuplicateCount.GetValueOrDefault(1));
                case ExceptionSorts.TimeAsc:
                    return source.OrderBy(e => e.CreationDate);
                //case ExceptionSorts.TimeDesc:
                default:
                    return source.OrderByDescending(e => e.CreationDate);
            }
        }

        public static List<Error> GetAllErrors(string appName = null, int maxPerApp = 5000, ExceptionSorts sort = ExceptionSorts.TimeDesc)
        {
            using (MiniProfiler.Current.Step("GetAllErrors() - All Stores" + (appName.HasValue() ? " (app:" + appName + ")" : "")))
            {
                var allErrors = Stores.SelectMany(s => s.GetErrorSummary(maxPerApp, appName));
                return GetSorted(allErrors, sort).ToList();
            }
        }

        public static async Task<List<Error>> GetSimilarErrorsAsync(Error error, bool byTime = false, int max = 200, ExceptionSorts sort = ExceptionSorts.TimeDesc)
        {
            if (error == null) return new List<Error>();
            var errorFetches = GetStores(error.ApplicationName).Select(s => byTime ? s.GetSimilarErrorsInTime(error, max) : s.GetSimilarErrors(error, max));
            var similarErrors = (await Task.WhenAll(errorFetches)).SelectMany(e => e);
            return GetSorted(similarErrors, sort).ToList();
        }

        public static async Task<List<Error>> FindErrorsAsync(string searchText, string appName = null, int max = 200, bool includeDeleted = false, ExceptionSorts sort = ExceptionSorts.TimeDesc)
        {
            if (searchText.IsNullOrEmpty()) return new List<Error>();
            var stores = appName.HasValue() ? GetStores(appName) : Stores;
            var errorFetches = stores.Select(s => s.FindErrors(searchText, appName, max, includeDeleted));
            var results = (await Task.WhenAll(errorFetches)).SelectMany(e => e);
            return GetSorted(results, sort).ToList();
        }
        
        private static List<Application> GetApplications()
        {
            using (MiniProfiler.Current.Step("GetApplications() - All Stores"))
            {
                var apps = Stores.SelectMany(s => s.Applications.SafeData(true)).ToList();
                if (!ConfigApplications.Any()) return apps;

                var result = ConfigApplications.ToList();
                for (var i = 0; i < result.Count; i++)
                {
                    var app = apps.FirstOrDefault(a => a.Name == result[i].Name);
                    if (app == null) continue;
                    result[i] = app;
                    apps.Remove(app);
                }
                result.AddRange(apps.OrderByDescending(a => a.ExceptionCount));
                return result;
            }
        }
    }
}