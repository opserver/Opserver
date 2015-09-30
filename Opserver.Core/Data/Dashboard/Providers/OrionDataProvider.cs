using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data.Dashboard.Providers
{
    public partial class OrionDataProvider : DashboardDataProvider<OrionSettings>
    {
        public override bool HasData => NodeCache.HasData();
        public string Host => Settings.Host;
        public string ConnectionString => Settings.ConnectionString;
        public int QueryTimeoutMs => Settings.QueryTimeoutMs;
        public override int MinSecondsBetweenPolls => 5;
        public override string NodeType => "Orion";

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return NodeCache;
            }
        }

        public OrionDataProvider(OrionSettings settings) : base(settings) {}

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
        
        private async Task<List<T>> UtilizationQuery<T>(string id, string allSql, string sampledSql, string dateField, DateTime? start, DateTime? end, int? pointCount)
        {
            using (var conn = await GetConnectionAsync())
            {
                return await conn.QueryAsync<T>(
                    (pointCount.HasValue ? sampledSql : allSql)
                        .Replace("{dateRange}", GetOptionalDateClause(dateField, start, end)),
                    new {id = int.Parse(id), start, end, intervals = pointCount});
            }
        }
    }
}
