using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data
{
    public class IssueProvider
    {
        private static readonly List<IIssuesProvider> IssueProviders;

        static IssueProvider()
        {
            IssueProviders = new List<IIssuesProvider>();
            var providers = AppDomain.CurrentDomain.GetAssemblies()
                                     .SelectMany(s => s.GetTypes())
                                     .Where(typeof (IIssuesProvider).IsAssignableFrom);
            foreach (var p in providers)
            {
                if (!p.IsClass) continue;
                try
                {
                    IssueProviders.Add((IIssuesProvider) Activator.CreateInstance(p));
                }
                catch (Exception e)
                {
                    Current.LogException("Error creating IIssuesProvider instance for " + p, e);
                }
            }
        }

        public static List<Issue> GetIssues()
        {
            using (MiniProfiler.Current.Step("GetIssues"))
                return Current.LocalCache.GetSet<List<Issue>>("IssuesList", (old, ctx) =>
                {
                    var result = new List<Issue>();
                    Parallel.ForEach(IssueProviders, p =>
                    {
                        List<Issue> pIssues;
                        using (MiniProfiler.Current.Step("Issues: " + p.Name))
                        {
                            pIssues = p.GetIssues().ToList();
                        }
                        lock (result)
                        {
                            result.AddRange(pIssues);
                        }
                    });

                    return result
                        .OrderByDescending(i => i.IsService)
                        .ThenByDescending(i => i.MonitorStatus)
                        .ThenByDescending(i => i.Date)
                        .ThenBy(i => i.Title)
                        .ToList();
                }, 60, 4*60*60);
        }
    }


    public interface IIssuesProvider
    {
        string Name { get; }
        IEnumerable<Issue> GetIssues();
    }

    public class Issue<T> : Issue where T : IMonitorStatus
    {
        public T Item { get; set; }
        public Issue(T item, DateTime? date = null)
        {
            Date = date ?? DateTime.UtcNow;
            Item = item;
            MonitorStatus = item.MonitorStatus;
            Description = item.MonitorStatusReason;
        }
        public Issue(T item, string title, DateTime? date = null) : this(item, date)
        {
            Title = title;
        }
    }

    public class Issue : IMonitorStatus
    {
        public string Title { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// Whether this issue is a service rather than a node - presumably an entire service being offline is worse
        /// </summary>
        public bool IsService { get; set; }
        public DateTime Date { get; set; }
        public MonitorStatus MonitorStatus { get; set; }
        public string MonitorStatusReason { get; set; }
    }
}
