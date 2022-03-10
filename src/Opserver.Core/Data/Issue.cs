using System;
using System.Collections.Generic;

namespace Opserver.Data
{
    public class Issue<T> : Issue where T : IMonitorStatus
    {
        public T Item { get; set; }

        public Issue(T item, string type, string title, DateTime? date = null)
        {
            Type = type;
            Title = title;
            Date = date ?? DateTime.UtcNow;
            Item = item;
            MonitorStatus = item.MonitorStatus;
            Description = item.MonitorStatusReason;
        }
    }

    public class Issue : IMonitorStatus
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// Whether this issue is a service rather than a node - presumably an entire service being offline is worse
        /// </summary>
        public bool IsCluster { get; set; }
        public DateTime Date { get; set; }
        public MonitorStatus MonitorStatus { get; set; }
        public string MonitorStatusReason => Description;
    }

    public interface IIssuesProvider
    {
        string Name { get; }
        IEnumerable<Issue> GetIssues();
    }
}
