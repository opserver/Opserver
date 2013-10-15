using System;
using System.Collections.Generic;
using System.Linq;
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

        public static Error GetError(string appName, Guid guid)
        {
            return GetStores(appName).Select(s => s.GetError(guid)).FirstOrDefault(e => e != null);
        }

        public static T Action<T>(string appName, Func<ExceptionStore, T> action)
        {
            var result = default(T);
            GetApps(appName).ForEach(a =>
                {
                    var aResult = action(a.Store);
                    if (typeof(T) == typeof(bool) && Convert.ToBoolean(aResult))
                    {
                        result = aResult;
                    }
                    if (a.Store != null)
                    {
                        a.Store.Applications.Purge();
                        a.Store.ErrorSummary.Purge();
                    }
                });
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

        public static List<Error> GetAllErrors(string appName = null, int maxPerApp = 1000)
        {
            using (MiniProfiler.Current.Step("GetAllErrors() - All Stores" + (appName.HasValue() ? " (app:" + appName + ")" : "")))
            {
                return
                    Stores.SelectMany(s => s.GetErrorSummary(maxPerApp, appName))
                          .OrderByDescending(e => e.CreationDate)
                          .ToList();
            }
        }

        public static List<Error> GetSimilarErrors(Error error, bool byTime = false, int max = 200)
        {
            if (error == null) return new List<Error>();
            return GetStores(error.ApplicationName).SelectMany(s => byTime ? s.GetSimilarErrorsInTime(error, max) : s.GetSimilarErrors(error, max)).OrderByDescending(e => e.CreationDate).ToList();
        }

        public static List<Error> FindErrors(string searchText, string appName = null, int max = 200, bool includeDeleted = false)
        {
            if (searchText.IsNullOrEmpty()) return new List<Error>();
            var stores = appName.HasValue() ? GetStores(appName) : Stores;
            return stores.SelectMany(s => s.FindErrors(searchText, appName, max, includeDeleted)).OrderByDescending(e => e.CreationDate).ToList();
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