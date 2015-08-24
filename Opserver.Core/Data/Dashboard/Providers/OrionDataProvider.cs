using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class OrionDataProvider : DashboardDataProvider
    {
        public override bool HasData => NodeCache.HasData();
        public string Host { get; private set; }
        public OrionDataProvider(string uniqueKey) : base(uniqueKey) { }
        public override int MinSecondsBetweenPolls => 5;
        public override string NodeType => "Orion";

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
        
        public Task<DbConnection> GetConnectionAsync()
        {
            return Connection.GetOpenAsync(ConnectionString, QueryTimeoutMs);
        }

        private string GetOptionalDateClause(string field, DateTime? start, DateTime? end)
        {
            if (start.HasValue && end.HasValue) // start & end
                return $"{field} Between @start and @end";
            if (start.HasValue) // no end
                return $"{field} >= @start";
            if (end.HasValue)
                return $"{field} <= @end";
            return "1 = 1";
        }
        
        private async Task<IEnumerable<T>> UtilizationQuery<T>(int id, string allSql, string sampledSql, string dateField, DateTime? start, DateTime? end, int? pointCount)
        {
            using (var conn = await GetConnectionAsync())
            {
                return await conn.QueryAsync<T>(
                    (pointCount.HasValue ? sampledSql : allSql)
                        .Replace("{dateRange}", GetOptionalDateClause(dateField, start, end)),
                    new {id, start, end, intervals = pointCount});
            }
        }
    }
}
