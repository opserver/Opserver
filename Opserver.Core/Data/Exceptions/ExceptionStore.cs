using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using StackExchange.Profiling;
using StackExchange.Exceptional;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data.Exceptions
{
    public class ExceptionStore : PollNode
    {
        public const int PerAppSummaryCount = 1000;

        public override string ToString() => "Store: " + Settings.Name;

        private int? QueryTimeout => Settings.QueryTimeoutMs;
        public string Name => Settings.Name;
        public string Description => Settings.Description;
        public ExceptionsSettings.Store Settings { get; internal set; }

        public override int MinSecondsBetweenPolls => 1;
        public override string NodeType => "Exceptions";

        public override IEnumerable<Cache> DataPollers
        {
            get
            {
                yield return Applications;
                yield return ErrorSummary;
            }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus() { yield break; }
        protected override string GetMonitorStatusReason() { return null; }
        
        public ExceptionStore(ExceptionsSettings.Store settings) : base(settings.Name)
        {
            Settings = settings;
        }

        private Cache<List<Application>> _applications;
        public Cache<List<Application>> Applications =>
            _applications ?? (_applications = new Cache<List<Application>>(
                this,
                "Exceptions Fetch: " + Name + ":" + nameof(Applications),
                Settings.PollIntervalSeconds,
                async () =>
                {
                    var result = await QueryListAsync<Application>($"Applications Fetch: {Name}", @"
Select ApplicationName as Name, 
       Sum(DuplicateCount) as ExceptionCount,
	   Sum(Case When CreationDate > DateAdd(Second, -@RecentSeconds, GETUTCDATE()) Then DuplicateCount Else 0 End) as RecentExceptionCount,
	   MAX(CreationDate) as MostRecent
  From Exceptions
 Where DeletionDate Is Null
 Group By ApplicationName", new {Current.Settings.Exceptions.RecentSeconds}).ConfigureAwait(false);
                    result.ForEach(a =>
                    {
                        a.StoreName = Name;
                        a.Store = this;
                    });
                    return result;
                },
                afterPoll: cache => ExceptionStores.UpdateApplicationGroups()));

        private Cache<List<Error>> _errorSummary;
        public Cache<List<Error>> ErrorSummary =>
            _errorSummary ?? (_errorSummary = new Cache<List<Error>>(
                this,
                "Exceptions Fetch: " + Name + ":" + nameof(ErrorSummary),
                Settings.PollIntervalSeconds,
                () => QueryListAsync<Error>($"ErrorSummary Fetch: {Name}", @"
Select e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.StatusCode, e.ErrorHash, e.DuplicateCount
  From (Select Id, Rank() Over (Partition By ApplicationName Order By CreationDate desc) as r
		From Exceptions
		Where DeletionDate Is Null) er
	   Inner Join Exceptions e On er.Id = e.Id
 Where er.r <= @PerAppSummaryCount
 Order By CreationDate Desc", new {PerAppSummaryCount})));

        public async Task<List<Error>> GetErrorSummary(int maxPerApp, string group = null, string app = null)
        {
            var errors = await ErrorSummary.GetData();
            if (errors == null) return new List<Error>();
            // specific application - this is most specific
            if (app.HasValue())
            {
                return errors.Where(e => e.ApplicationName == app)
                             .Take(maxPerApp)
                             .ToList();
            }
            if (group.HasValue())
            {
                var apps = ExceptionStores.GetAppNames(group, app).ToHashSet();
                return errors.GroupBy(e => e.ApplicationName)
                             .Where(g => apps.Contains(g.Key))
                             .SelectMany(e => e.Take(maxPerApp))
                             .ToList();

            }
            // all apps, 1000
            if (maxPerApp == PerAppSummaryCount)
            {
                return errors;
            }
            // app apps, n records
            return errors.GroupBy(e => e.ApplicationName)
                         .SelectMany(e => e.Take(maxPerApp))
                         .ToList();
        }

        /// <summary>
        /// Get all current errors, possibly per application
        /// </summary>
        /// <remarks>This does not populate Detail, it's comparatively large and unused in list views</remarks>
        public Task<List<Error>> GetAllErrorsAsync(int maxPerApp, IEnumerable<string> apps = null)
        {
            return QueryListAsync<Error>($"{nameof(GetAllErrorsAsync)}() for {Name}", @"
Select e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.StatusCode, e.ErrorHash, e.DuplicateCount
  From (Select Id, Rank() Over (Partition By ApplicationName Order By CreationDate desc) as r
		From Exceptions
		Where DeletionDate Is Null" + (apps != null && apps.Any() ? " And ApplicationName In @apps" : "") + @") er
	   Inner Join Exceptions e On er.Id = e.Id
 Where er.r <= @maxPerApp
 Order By CreationDate Desc", new {maxPerApp, apps});
        }

        public Task<List<Error>> GetSimilarErrorsAsync(Error error, int max)
        {
            return QueryListAsync<Error>($"{nameof(GetSimilarErrorsAsync)}() for {Name}", @"
    Select Top (@max) e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where ApplicationName = @ApplicationName
       And Message = @Message
     Order By CreationDate Desc", new {max, error.ApplicationName, error.Message});
        }

        public Task<List<Error>> GetSimilarErrorsInTimeAsync(Error error, int max)
        {
            return QueryListAsync<Error>($"{nameof(GetSimilarErrorsInTimeAsync)}() for {Name}", @"
    Select Top (@max) e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where CreationDate Between @start and @end
     Order By CreationDate Desc", new { max, start = error.CreationDate.AddMinutes(-5), end = error.CreationDate.AddMinutes(5) });
        }

        public Task<List<Error>> FindErrorsAsync(string searchText, int max, bool includeDeleted, IEnumerable<string> apps = null)
        {
            return QueryListAsync<Error>($"{nameof(FindErrorsAsync)}() for {Name}", @"
    Select Top (@max) e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where (Message Like @search Or Detail Like @search Or Url Like @search)" + (apps != null && apps.Any() ? " And ApplicationName In @apps" : "") + (includeDeleted ? "" : " And DeletionDate Is Null") + @"
     Order By CreationDate Desc", new { search = "%" + searchText + "%", apps, max });
        }

        public Task<int> DeleteAllErrorsAsync(List<string> apps)
        {
            return ExecTaskAsync($"{nameof(DeleteAllErrorsAsync)}() for {Name}", @"
Update Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName In @apps", new { apps });
        }

        public Task<int> DeleteSimilarErrorsAsync(Error error)
        {
            return ExecTaskAsync($"{nameof(DeleteSimilarErrorsAsync)}('{error.GUID.ToString()}') (app: {error.ApplicationName}) for {Name}", @"
Update Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where ApplicationName = @ApplicationName
   And Message = @Message
   And DeletionDate Is Null
   And IsProtected = 0", new {error.ApplicationName, error.Message});
        }

        public Task<int> DeleteErrorsAsync(List<Guid> ids)
        {
            return ExecTaskAsync($"{nameof(DeleteErrorsAsync)}({ids.Count.ToString()} Guids) for {Name}", @"
Update Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And GUID In @ids", new { ids });
        }
        
        public async Task<Error> GetErrorAsync(Guid guid)
        {
            try
            {
                Error sqlError;
                using (MiniProfiler.Current.Step(nameof(GetErrorAsync) + "() (guid: " + guid.ToString() + ") for " + Name))
                using (var c = await GetConnectionAsync().ConfigureAwait(false))
                {
                    sqlError = await c.QueryFirstOrDefaultAsync<Error>(@"
    Select Top 1 * 
      From Exceptions 
     Where GUID = @guid", new { guid }, commandTimeout: QueryTimeout).ConfigureAwait(false);
                }
                if (sqlError == null) return null;

                // everything is in the JSON, but not the columns and we have to deserialize for collections anyway
                // so use that deserialized version and just get the properties that might change on the SQL side and apply them
                var result = Error.FromJson(sqlError.FullJson);
                result.DuplicateCount = sqlError.DuplicateCount;
                result.DeletionDate = sqlError.DeletionDate;
                result.ApplicationName = sqlError.ApplicationName;
                return result;
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return null;
            }
        }

        public async Task<bool> ProtectErrorAsync(Guid guid)
        {
              return await ExecTaskAsync($"{nameof(ProtectErrorAsync)}() (guid: {guid.ToString()}) for {Name}", @"
Update Exceptions 
   Set IsProtected = 1, DeletionDate = Null
 Where GUID = @guid", new {guid}).ConfigureAwait(false) > 0;
        }

        public async Task<bool> DeleteErrorAsync(Guid guid)
        {
            return await ExecTaskAsync($"{nameof(DeleteErrorAsync)}() (guid: {guid.ToString()}) for {Name}", @"
Update Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where GUID = @guid 
   And DeletionDate Is Null", new { guid }).ConfigureAwait(false) > 0;
        }

        public async Task<List<T>> QueryListAsync<T>(string step, string sql, dynamic paramsObj)
        {
            try
            {
                using (MiniProfiler.Current.Step(step))
                using (var c = await GetConnectionAsync().ConfigureAwait(false))
                {
                    return await c.QueryAsync<T>(sql, paramsObj as object, commandTimeout: QueryTimeout).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return new List<T>();
            }
        }

        public async Task<int> ExecTaskAsync(string step, string sql, dynamic paramsObj)
        {
            using (MiniProfiler.Current.Step(step))
            using (var c = await GetConnectionAsync().ConfigureAwait(false))
            {
                return await c.ExecuteAsync(sql, paramsObj as object, commandTimeout: QueryTimeout).ConfigureAwait(false);
            }
        }

        private Task<DbConnection> GetConnectionAsync() =>
            Connection.GetOpenAsync(Settings.ConnectionString, QueryTimeout);
    }
}