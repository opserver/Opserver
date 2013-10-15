using System;
using System.Collections.Generic;
using System.Data.Common;
using Dapper;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class OrionDataProvider : DashboardDataProvider
    {
        public override bool HasData { get { return NodeCache.HasData(); } }
        public string Host { get; private set; }
        public OrionDataProvider(string uniqueKey) : base(uniqueKey) { }
        public override int MinSecondsBetweenPolls { get { return 5; } }
        public override string NodeType { get { return "Orion"; } }
        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return NodeCache;
                yield return InterfaceCache;
                yield return VolumeCache;
                yield return ApplicationCache;
                yield return NodeIPCache;
            }
        }

        public OrionDataProvider(DashboardSettings.Provider provider) : base(provider)
        {
            Host = provider.Host;
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }
        
        public DbConnection GetConnection()
        {
            return Connection.GetOpen(ConnectionString, QueryTimeoutMs);
        }

        private string GetOptionalDateClause(string field, DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue) // start & end
                return string.Format("{0} Between @start and @end", field);
            if (start.HasValue) // no end
                return string.Format("{0} >= @start", field);
            if (end.HasValue)
                return string.Format("{0} <= @end", field);
            return "1 = 1";
        }
        
        private IEnumerable<T> UtilizationQuery<T>(int id, string allSql, string sampledSql, string dateField, DateTime? start, DateTime? end, int? pointCount)
        {
            using (var conn = GetConnection())
            {
                return conn.Query<T>(
                    (pointCount.HasValue ? sampledSql : allSql)
                        .Replace("{dateRange}", GetOptionalDateClause(dateField, start, end)),
                    new { id, start, end, intervals = pointCount });
            }
        }
    }
}
