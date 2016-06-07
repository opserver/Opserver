using StackExchange.Exceptional;
using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Exceptions
{
    public class MysqlExceptionStore : PollNode, IExceptionStore
    {
        public const int PerAppSummaryCount = 1000;

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

        public MysqlExceptionStore(ExceptionsSettings.Store settings) : base(settings.Name)
        {
            Settings = settings;
        }

        public Func<Cache<T>, Task> UpdateFromSql<T>(string opName, Func<Task<T>> getFromConnection) where T : class
        {
            return UpdateCacheItem("Exceptions Fetch: " + Name + ":" + opName,
                getFromConnection,
                addExceptionData: e => e.AddLoggedData("Server", Name));
        }

        private Cache<List<Application>> _applications;
        public Cache<List<Application>> Applications
        {
            get
            {
                return _applications ?? (_applications = new Cache<List<Application>>
                {
                    CacheForSeconds = Settings.PollIntervalSeconds,
                    UpdateCache = UpdateFromSql("Applications-List",
                            async () =>
                            {
                                var result = (await QueryListAsync<Application>($"Applications Fetch: {Name}", @"Select ApplicationName as Name, 
       Sum(DuplicateCount) as ExceptionCount,
	   Sum(Case When CreationDate > Date_Add(UTC_DATE()+utc_time(),INTERVAL -@RecentSeconds SECOND) Then DuplicateCount Else 0 End) as RecentExceptionCount,
	   MAX(CreationDate) as MostRecent
  From Exceptions
 Where DeletionDate Is Null
 Group By ApplicationName", new { Current.Settings.Exceptions.RecentSeconds }));
                                result.ForEach(a => { a.StoreName = Name; a.Store = this; });
                                return result;
                            })
                });
            }
        }

        private Cache<List<Error>> _errorSummary;
        public Cache<List<Error>> ErrorSummary
        {
            get
            {
                return _errorSummary ?? (_errorSummary = new Cache<List<Error>>
                {
                    CacheForSeconds = Settings.PollIntervalSeconds,
                    UpdateCache = UpdateFromSql("Error-Summary-List",
                        () => QueryListAsync<Error>($"ErrorSummary Fetch: {Name}", @" SET @rownum:= 0;
Select e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.StatusCode, e.ErrorHash, e.DuplicateCount
  From (Select Id,  (SELECT @rownum := @rownum + 1)as r
		From Exceptions
		Where DeletionDate Is Null order by CreationDate desc) er
	   Inner Join Exceptions e On er.Id = e.Id
 Where er.r <= @PerAppSummaryCount
 Order By CreationDate Desc", new { PerAppSummaryCount }))
                });
            }
        }

        public List<Error> GetErrorSummary(int maxPerApp, string appName = null)
        {
            var errors = ErrorSummary.SafeData(true);
            // specific application
            if (appName.HasValue())
            {
                return errors.Where(e => e.ApplicationName == appName)
                             .Take(maxPerApp)
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
        public Task<List<Error>> GetAllErrors(int maxPerApp, string appName = null)
        {
            return QueryListAsync<Error>($"GetAllErrors() for {Name} App: {(appName ?? "All")}", @"SET @rownum := 0;
Select e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.StatusCode, e.ErrorHash, e.DuplicateCount
  From (Select Id, (SELECT @rownum := @rownum + 1)as r
		From Exceptions
		Where DeletionDate Is Null" + (appName.HasValue() ? " And ApplicationName = @appName" : "") + @" order by CreationDate desc ) er
	   Inner Join Exceptions e On er.Id = e.Id
 Where er.r <= @maxPerApp
 Order By CreationDate Desc", new { maxPerApp, appName });
        }

        public Task<List<Error>> GetSimilarErrorsAsync(Error error, int max)
        {
            return QueryListAsync<Error>($"GetSimilarErrors() for {Name}", @"
    Select e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where ApplicationName = @ApplicationName
       And Message = @Message
     Order By CreationDate Desc LIMIT 0,@max", new { max, error.ApplicationName, error.Message });
        }

        public Task<List<Error>> GetSimilarErrorsInTimeAsync(Error error, int max)
        {
            return QueryListAsync<Error>($"GetSimilarErrorsInTime() for {Name}", @"
    Select  e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where CreationDate Between @start and @end
     Order By CreationDate Desc LIMIT 0,@max", new { max, start = error.CreationDate.AddMinutes(-5), end = error.CreationDate.AddMinutes(5) });
        }

        public Task<List<Error>> FindErrorsAsync(string searchText, string appName, int max, bool includeDeleted)
        {
            return QueryListAsync<Error>($"FindErrors() for {Name}", @"
    Select  e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where (Message Like @search Or Detail Like @search Or Url Like @search)" + (appName.HasValue() ? " And ApplicationName = @appName" : "") + (includeDeleted ? "" : " And DeletionDate Is Null") + @"
     Order By CreationDate Desc LIMIT 0,@max", new { search = '%' + searchText + '%', appName, max });
        }

        public Task<int> DeleteAllErrorsAsync(string appName)
        {
            return ExecTask($"DeleteAllErrors() (app: {appName}) for {Name}", @"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @appName", new { appName });
        }

        public Task<int> DeleteSimilarErrorsAsync(Error error)
        {
            return ExecTask($"DeleteSimilarErrors('{error.GUID}') (app: {error.ApplicationName}) for {Name}", @"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where ApplicationName = @ApplicationName
   And Message = @Message
   And DeletionDate Is Null
   And IsProtected = 0", new { error.ApplicationName, error.Message });
        }

        public Task<int> DeleteErrorsAsync(List<Guid> ids)
        {
            return ExecTask($"DeleteErrors({ids.Count} Guids) () for {Name}", @"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @appName
   And GUID In @ids", new {  ids });
        }

        public async Task<Error> GetErrorAsync(Guid guid)
        {
            try
            {
                Error sqlError;
                using (MiniProfiler.Current.Step("GetError() (guid: " + guid + ") for " + Name))
                using (var c = await GetConnectionAsync())
                {
                    sqlError = (await c.QueryAsync<Error>(@"
    Select  * 
      From Exceptions 
     Where GUID = @guid LIMIT 0,1", new { guid }, commandTimeout: QueryTimeout)).FirstOrDefault();
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
            return await ExecTask($"ProtectError() (guid: {guid}) for {Name}", @"
Update Exceptions 
   Set IsProtected = 1, DeletionDate = Null
 Where GUID = @guid", new { guid }) > 0;
        }

        public async Task<bool> DeleteErrorAsync(Guid guid)
        {
            return await ExecTask($"DeleteError() (guid: {guid}) for {Name}", @"
Update Exceptions 
   Set DeletionDate = UTC_DATE() 
 Where GUID = @guid 
   And DeletionDate Is Null", new { guid }) > 0;
        }

        public async Task<List<T>> QueryListAsync<T>(string step, string sql, dynamic paramsObj)
        {
            try
            {
                using (MiniProfiler.Current.Step(step))
                using (var c = await GetConnectionAsync())
                {
                    return await c.QueryAsync<T>(sql, paramsObj as object, commandTimeout: QueryTimeout);
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return new List<T>();
            }
        }

        public async Task<int> ExecTask(string step, string sql, dynamic paramsObj)
        {
            using (MiniProfiler.Current.Step(step))
            using (var c = await GetConnectionAsync())
            {
                return await c.ExecuteAsync(sql, paramsObj as object, commandTimeout: QueryTimeout);
            }
        }

        private Task<DbConnection> GetConnectionAsync()
        {
            return Helpers.Connection.GetOpenAsync(Settings.ConnectionString, QueryTimeout, Settings.DatabaseType);
        }

       

      

    }
}
