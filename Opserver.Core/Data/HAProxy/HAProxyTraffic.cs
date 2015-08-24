using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data.HAProxy
{
    public class HAProxyTraffic
    {
        public static string ConnectionString
        {
            get
            {
                var fc = Current.Settings.HAProxy.Traffic.Connections.FirstOrDefault();
                return fc?.ConnectionString;
            }
        }

        public static async Task<List<string>> GetHosts()
        {
            const string cacheKey = "host-list";
            var results = Current.LocalCache.Get<List<string>>(cacheKey);

            if (results == null)
            {
                const string sql =
                    @"
Select Host
From Logs_Summary
Where CreationDate > GETUTCDATE() - 15
Group By Host
Having Sum(Hits) > 5000
Order By 1";
                using (var conn = await Connection.GetOpenAsync(ConnectionString))
                {
                    results = await conn.QueryAsync<string>(sql);
                }
                results.RemoveAll(h => !IsValidHost(h));
                Current.LocalCache.Set(cacheKey, results, 5 * 60 * 60); // cache for 5 hours, this *very* rarely changes
            }
            return results;
        }

        private static bool IsValidHost(string host)
        {
            if (!host.Contains(".com") && !host.Contains(".net")) return false;
            if (host.Contains(":")) return false;

            return true;
        }

        public static Task<List<TrafficDay>> GetTrafficSummary(int lastNdays, string host = null)
        {
            return GetTrafficSummary(host, DateTime.UtcNow.AddDays(-lastNdays), DateTime.UtcNow);
        }

        public static async Task<List<TrafficDay>> GetTrafficSummary(string host, DateTime? startDate, DateTime? endDate)
        {
            var cacheKey = "haproxy-traffic-summary-" + host.IsNullOrEmptyReturn("*");

            var sql = @"
Select CreationDate, 
       Sum(Cast(Hits as BigInt)) as Hits,
       Sum(Case IsPageView When 1 Then Cast(Hits as BigInt) Else 0 End) as PageHits
From Logs_Summary" + (host.HasValue() ? @"
Where Host = @host" : "") + @"
Group By CreationDate
Order By CreationDate";

            var results = Current.LocalCache.Get<List<TrafficDay>>(cacheKey);
            if (results == null)
            {
                using (var conn = await Connection.GetOpenAsync(ConnectionString))
                {
                    results = await conn.QueryAsync<TrafficDay>(sql, new { host, start = startDate, end = endDate });
                    Current.LocalCache.Set(cacheKey, results, 60 * 60); // cache for an hour, that's the SQL recalc interval
                }
            }
            if (!startDate.HasValue && !endDate.HasValue) return results;
            
            // filter! like brita but with more enumerables and lambdatastic goodness
            IEnumerable<TrafficDay> fResults = results;
            if (startDate.HasValue) fResults = fResults.Where(td => td.CreationDate >= startDate);
            if (endDate.HasValue) fResults = fResults.Where(td => td.CreationDate <= endDate);
            return fResults.ToList();
        }

        public static async Task<List<RouteHit>> GetTopPageRotues(int lastNdays, string host = null)
        {
            var cacheKey = "top-page-routes-" + lastNdays + "-" + host;
            var results = Current.LocalCache.Get<List<RouteHit>>(cacheKey);

            if (results == null)
            {
                var sql = @"
Select RouteName, 
       Sum(Cast(Hits as BigInt)) as Hits,
       Sum(Cast(Bytes as BigInt)) as Bytes,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(Tr as BigInt) Else Null End) Tr24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(SqlCount as BigInt) Else Null End) SqlCount24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(SqlDurationMs as BigInt) Else Null End) SqlDurationMs24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(RedisCount as BigInt) Else Null End) RedisCount24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(RedisDurationMs as BigInt) Else Null End) RedisDurationMs24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(HttpCount as BigInt) Else Null End) HttpCount24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(HttpDurationMs as BigInt) Else Null End) HttpDurationMs24Hrs,       
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(TagEngineCount as BigInt) Else Null End) TagEngineCount24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(TagEngineDurationMs as BigInt) Else Null End) TagEngineDurationMs24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(AspNetDurationMs as BigInt) Else Null End) AspNetDurationMs24Hrs,
       Sum(Case When CreationDate > GETUTCDATE() - 1 Then Cast(Hits as BigInt) Else Null End) Hits24Hrs
From Logs_Summary
Where RouteName Is Not Null
  And IsPageView = 1" + (host.HasValue() ? @"
  And Host = @host" : "") + @"
  And CreationDate > GETUTCDATE() - @lastNdays
  And ResponseCode = 200
Group By RouteName
Order By Sum(Hits) Desc";

                using (var conn = await Connection.GetOpenAsync(ConnectionString))
                {
                    results = await conn.QueryAsync<RouteHit>(sql, new {lastNdays, host});
                }
                Current.LocalCache.Set(cacheKey, results, 60 * 60); // cache for an hour, this only aggregates in sql once an hour
            }
            return results;
        }

        public static async Task<List<RouteData>> GetRouteData(string routeName, int? lastNdays = null, string server = null, string host = null)
        {
            var sql = @"
Select CreationDate, 
       Sum(Cast(Hits as BigInt)) as Hits, 
       Sum(Cast(Bytes as BigInt)) as Bytes,
       Sum(Cast(Tr as BigInt)) as Tr,
       Sum(Cast(SqlCount as BigInt)) as SqlCount,
       Sum(Cast(SqlDurationMs as BigInt)) as SqlDurationMs,
       Sum(Cast(RedisCount as BigInt)) as RedisCount,
       Sum(Cast(RedisDurationMs as BigInt)) as RedisDurationMs,
       Sum(Cast(HttpCount as BigInt)) as HttpCount,
       Sum(Cast(HttpDurationMs as BigInt)) as HttpDurationMs,
       Sum(Cast(TagEngineCount as BigInt)) as TagEngineCount,
       Sum(Cast(TagEngineDurationMs as BigInt)) as TagEngineDurationMs,
       Sum(Cast(AspNetDurationMs as BigInt)) as AspNetDurationMs
From Logs_Summary
Where ResponseCode = 200
  And IsPageView = 1
  And RouteName = @routeName" + (host.HasValue() ? @"
  And Host = @host" : "") + (server.HasValue() ? @"
  And Server = @server" : "") + (lastNdays.HasValue ? @"
  And CreationDate > GETUTCDATE() - @lastNdays " : "") + @"  
Group By CreationDate
Order By CreationDate";

            using (var conn = await Connection.GetOpenAsync(ConnectionString))
            {
                return await conn.QueryAsync<RouteData>(sql, new { routeName, host, lastNdays, server });
            }
        }

        public class TrafficDay
        {
            public DateTime CreationDate { get; internal set; }
            public long Hits { get; internal set; }
            public long PageHits { get; internal set; }
        }

        public class RouteHit
        {
            public string RouteName { get; internal set; }
            public long? Hits { get; internal set; }
            public long? Bytes { get; internal set; }

            public long? Tr24Hrs { get; internal set; }
            public long? SqlCount24Hrs { get; internal set; }
            public long? SqlDurationMs24Hrs { get; internal set; }
            public long? RedisCount24Hrs { get; internal set; }
            public long? RedisDurationMs24Hrs { get; internal set; }
            public long? HttpCount24Hrs { get; internal set; }
            public long? HttpDurationMs24Hrs { get; internal set; }
            public long? TagEngineCount24Hrs { get; internal set; }
            public long? TagEngineDurationMs24Hrs { get; internal set; }
            public long? AspNetDurationMs24Hrs { get; internal set; }
            public long? Hits24Hrs { get; internal set; }

            private readonly Func<long?, long?, decimal?> _getAvg = (v, h) => h > 0 && v.HasValue ? v/h : null;

            public decimal? AvgBytes => _getAvg(Bytes, Hits);
            public decimal? AvgTr24Hrs => _getAvg(Tr24Hrs, Hits24Hrs);
            public decimal? AvgSqlCount24Hrs => _getAvg(SqlCount24Hrs, Hits24Hrs);
            public decimal? AvgSqlDurationMs24Hrs => _getAvg(SqlDurationMs24Hrs, Hits24Hrs);
            public decimal? AvgRedisCount24Hrs => _getAvg(RedisCount24Hrs, Hits24Hrs);
            public decimal? AvgRedisDurationMs24Hrs => _getAvg(RedisDurationMs24Hrs, Hits24Hrs);
            public decimal? AvgHttpCount24Hrs => _getAvg(HttpCount24Hrs, Hits24Hrs);
            public decimal? AvgHttpDurationMs24Hrs => _getAvg(HttpDurationMs24Hrs, Hits24Hrs);
            public decimal? AvgTagEngineCount24Hrs => _getAvg(TagEngineCount24Hrs, Hits24Hrs);
            public decimal? AvgTagEngineDurationMs24Hrs => _getAvg(TagEngineDurationMs24Hrs, Hits24Hrs);
            public decimal? AvgAspNetDurationMs24Hrs => _getAvg(AspNetDurationMs24Hrs, Hits24Hrs);

            public decimal? AvgCalculatedAspNetDurationMs => _getAvg(AspNetDurationMs24Hrs - SqlDurationMs24Hrs - RedisDurationMs24Hrs - HttpDurationMs24Hrs - TagEngineDurationMs24Hrs, Hits);
            public decimal? AvgCalculatedOtherDurationMs => _getAvg(Tr24Hrs - AspNetDurationMs24Hrs, Hits);
        }

        public class RouteData
        {
            public DateTime CreationDate { get; internal set; }
            public long? Hits { get; internal set; }
            public long? Bytes { get; internal set; }
            public long? Tr { get; internal set; }
            public long? SqlCount { get; internal set; }
            public long? SqlDurationMs { get; internal set; }
            public long? RedisCount { get; internal set; }
            public long? RedisDurationMs { get; internal set; }
            public long? HttpCount { get; internal set; }
            public long? HttpDurationMs { get; internal set; }
            public long? TagEngineCount { get; internal set; }
            public long? TagEngineDurationMs { get; internal set; }
            public long? AspNetDurationMs { get; internal set; }

            private readonly Func<long?, long?, decimal?> _getAvg = (v, h) => h > 0 && v.HasValue ? v / h : null;

            public decimal? AvgBytes => _getAvg(Bytes, Hits);
            public decimal? AvgTr => _getAvg(Tr, Hits);
            public decimal? AvgSqlCount => _getAvg(SqlCount, Hits);
            public decimal? AvgSqlDurationMs => _getAvg(SqlDurationMs, Hits);
            public decimal? AvgRedisCount => _getAvg(RedisCount, Hits);
            public decimal? AvgRedisDurationMs => _getAvg(RedisDurationMs, Hits);
            public decimal? AvgHttpCount => _getAvg(HttpCount, Hits);
            public decimal? AvgHttpDurationMs => _getAvg(HttpDurationMs, Hits);
            public decimal? AvgTagEngineCount => _getAvg(TagEngineCount, Hits);
            public decimal? AvgTagEngineDurationMs => _getAvg(TagEngineDurationMs, Hits);
            public decimal? AvgAspNetDurationMs => _getAvg(AspNetDurationMs, Hits);

            public decimal? AvgCalculatedAspNetDurationMs => _getAvg(AspNetDurationMs - SqlDurationMs - RedisDurationMs - HttpDurationMs - TagEngineDurationMs, Hits);
            public decimal? AvgCalculatedOtherDurationMs => _getAvg(Tr - AspNetDurationMs, Hits);
        }
    }
}