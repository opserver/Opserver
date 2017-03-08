using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Data
{
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
        internal static ConcurrentBag<IIssuesProvider> Providers { get; } = new ConcurrentBag<IIssuesProvider>();

        public string Title { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// Whether this issue is a service rather than a node - presumably an entire service being offline is worse
        /// </summary>
        public bool IsCluster { get; set; }
        public DateTime Date { get; set; }
        public MonitorStatus MonitorStatus { get; set; }
        public string MonitorStatusReason => Description;

        public static List<Issue> GetAll()
        {
            using (MiniProfiler.Current.Step(nameof(GetAll)))
            {
                return Current.LocalCache.GetSet<List<Issue>>("IssuesList", (old, ctx) =>
                {
                    var result = new List<Issue>();
                    Parallel.ForEach(Providers, p =>
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
                        .OrderByDescending(i => i.IsCluster)
                        .ThenByDescending(i => i.MonitorStatus)
                        .ThenByDescending(i => i.Date)
                        .ThenBy(i => i.Title)
                        .ToList();
                }, 15.Seconds(), 4.Hours());
            }
        }
    }

    public static class IssuesExtensions
    {
        public static void Register(this IIssuesProvider provider)
        {
            Issue.Providers.Add(provider);
        }
    }

    public interface IIssuesProvider
    {
        string Name { get; }
        IEnumerable<Issue> GetIssues();
    }
}
