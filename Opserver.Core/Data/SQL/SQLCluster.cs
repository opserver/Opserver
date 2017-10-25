﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace StackExchange.Opserver.Data.SQL
{
    public partial class SQLCluster : IEquatable<SQLCluster>, IMonitedService
    {
        public string Name => Settings.Name;
        public string Description => Settings.Description;
        public TimeSpan RefreshInterval { get; }
        private SQLSettings.Cluster Settings { get; }

        public List<SQLNode> Nodes { get; }

        public List<SQLNode.AGInfo> AvailabilityGroups
        {
            get { return Nodes.SelectMany(n => n.AvailabilityGroups.Data?.Where(ag => ag.IsPrimaryReplica) ?? Enumerable.Empty<SQLNode.AGInfo>()).ToList(); }
        }

        public IEnumerable<SQLNode.AGInfo> GetAvailabilityGroups(string node, string agName)
        {
            bool agMatch(SQLNode.AGInfo ag) => agName.IsNullOrEmpty() || ag.Name == agName;

            return (node.HasValue()
                ? Nodes.Where(n => string.Equals(n.Name, node))
                : Nodes)
                .SelectMany(n => n.AvailabilityGroups.Data?.Where(agMatch) ?? Enumerable.Empty<SQLNode.AGInfo>());
        }

        public MonitorStatus MonitorStatus => Nodes.GetWorstStatus("SQLCluster-" + Name);
        public string MonitorStatusReason => MonitorStatus == MonitorStatus.Good ? null : Nodes.GetReasonSummary();

        public SQLNode.AGClusterState ClusterStatus =>
            Nodes.Find(n => n.AGClusterInfo.Data?.ClusterName.HasValue() ?? false)?.AGClusterInfo.Data;

        public QuorumTypes QuorumType => ClusterStatus?.QuorumType ?? QuorumTypes.Unknown;
        public QuorumStates QuorumState => ClusterStatus?.QuorumState ?? QuorumStates.Unknown;

        public SQLCluster(SQLSettings.Cluster cluster)
        {
            Settings = cluster;
            Nodes = cluster.Nodes
                           .Select(n => new SQLNode(this, n))
                           .Where(n => n.TryAddToGlobalPollers())
                           .ToList();
            RefreshInterval = (cluster.RefreshIntervalSeconds ?? Current.Settings.SQL.RefreshIntervalSeconds).Seconds();
        }

        public bool Equals(SQLCluster other) =>
            other != null && string.Equals(Name, other.Name);

        public SQLNode GetNode(string name) =>
            Nodes.Find(n => string.Equals(n.Name, name, StringComparison.InvariantCultureIgnoreCase));
    }
}