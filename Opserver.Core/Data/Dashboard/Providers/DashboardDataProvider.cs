﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public abstract class DashboardDataProvider<TSettings> : DashboardDataProvider where TSettings : class, IProviderSettings
    {
        public TSettings Settings { get; protected set; }

        protected DashboardDataProvider(TSettings settings) : base(settings)
        {
            Settings = settings;
        }
    }

    public abstract class DashboardDataProvider : PollNode, IIssuesProvider
    {
        public abstract bool HasData { get; }
        public string Name { get; protected set; }

        public virtual IEnumerable<Issue> GetIssues()
        {
            foreach (var n in AllNodes)
            {
                if (n.Issues?.Count > 0)
                {
                    foreach (var i in n.Issues)
                    {
                        yield return i;
                    }
                }
            }
        }

        public override string ToString() => GetType().Name;

        protected DashboardDataProvider(string uniqueKey) : base(uniqueKey) { }

        protected DashboardDataProvider(IProviderSettings settings) : base(settings.Name + "Dashboard")
        {
            Name = settings.Name;
        }

        /// <summary>
        /// Returns the current exceptions for this data provider
        /// </summary>
        public virtual IEnumerable<string> GetExceptions()
        {
            foreach (var p in DataPollers)
            {
                if (p.ErrorMessage.HasValue())
                    yield return p.ParentMemberName + ": " + p.ErrorMessage;
            }
        }

        #region Nodes

        public abstract List<Node> AllNodes { get; }

        public Node GetNodeById(string id) => AllNodes.Find(s => s.Id == id);

        public Node GetNodeByHostname(string hostName)
        {
            if (!Current.Settings.Dashboard.Enabled || hostName.IsNullOrEmpty()) return null;
            return AllNodes.Find(s => s.Name.IndexOf(hostName, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }

        public virtual IEnumerable<Node> GetNodesByIP(IPAddress ip) =>
            AllNodes.Where(n => n.IPs?.Any(i => i.Contains(ip)) == true);

        public virtual string GetManagementUrl(Node node) { return null; }
        public abstract Task<List<GraphPoint>> GetCPUUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null);
        public abstract Task<List<GraphPoint>> GetMemoryUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null);
        public abstract Task<List<DoubleGraphPoint>> GetNetworkUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null);
        public abstract Task<List<DoubleGraphPoint>> GetVolumePerformanceUtilizationAsync(Node node, DateTime? start, DateTime? end, int? pointCount = null);

        public abstract Task<List<DoubleGraphPoint>> GetUtilizationAsync(Interface iface, DateTime? start, DateTime? end, int? pointCount = null);

        public abstract Task<List<GraphPoint>> GetUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null);
        public abstract Task<List<DoubleGraphPoint>> GetPerformanceUtilizationAsync(Volume volume, DateTime? start, DateTime? end, int? pointCount = null);

        public Application GetApplication(string id) => AllNodes.SelectMany(n => n.Apps.Where(a => a.Id == id)).FirstOrDefault();

        #endregion

        protected Cache<T> ProviderCache<T>(
            Func<Task<T>> fetch,
            TimeSpan cacheDuration,
            TimeSpan? cacheFailureDuration = null,
            bool affectsStatus = true,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
            where T : class
        {
            return new Cache<T>(this,
                "Data Provieder Fetch: " + NodeType + ":" + typeof(T).Name,
                cacheDuration,
                fetch,
                addExceptionData: e => e.AddLoggedData("NodeType", NodeType),
                memberName: memberName,
                sourceFilePath: sourceFilePath,
                sourceLineNumber: sourceLineNumber)
            {
                AffectsNodeStatus = affectsStatus,
                CacheFailureDuration = cacheFailureDuration
            };
        }
    }
}
