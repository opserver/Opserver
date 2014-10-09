using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Dapper;
using StackExchange.Profiling;
using StackExchange.Exceptional;
using StackExchange.Opserver.Helpers;

namespace StackExchange.Opserver.Data.Exceptions
{
    public class ExceptionStore : PollNode
    {
        public const int PerAppSummaryCount = 1000;

        private int? QueryTimeout { get { return Settings.QueryTimeoutMs; } }
        public string Name { get { return Settings.Name; } }
        public string Description { get { return Settings.Description; } }
        public ExceptionsSettings.Store Settings { get; internal set; }

        public override int MinSecondsBetweenPolls { get { return 1; } }
        public override string NodeType { get { return "Exceptions"; } }

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

        public Action<Cache<T>> UpdateFromSql<T>(string opName, Func<DbConnection, T> getFromConnection) where T : class
        {
            return UpdateCacheItem("Exceptions Fetch: " + Name + ":" + opName,
                                   () => { using (var conn = GetConnection()) return getFromConnection(conn); },
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
                            conn => conn.Query<Application>(@"
Select ApplicationName as Name, 
       Sum(DuplicateCount) as ExceptionCount,
	   Sum(Case When CreationDate > DateAdd(Second, -@RecentSeconds, GETUTCDATE()) Then DuplicateCount Else 0 End) as RecentExceptionCount,
	   MAX(CreationDate) as MostRecent
  From Exceptions
 Where DeletionDate Is Null
 Group By ApplicationName",
                          new { Current.Settings.Exceptions.RecentSeconds }, commandTimeout: QueryTimeout)
                         .ForEach(a => { a.StoreName = Name; a.Store = this; }).ToList())
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
                        conn => conn.Query<Error>(@"
Select e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.StatusCode, e.ErrorHash, e.DuplicateCount
  From (Select Id, Rank() Over (Partition By ApplicationName Order By CreationDate desc) as r
		From Exceptions
		Where DeletionDate Is Null) er
	   Inner Join Exceptions e On er.Id = e.Id
 Where er.r <= @PerAppSummaryCount
 Order By CreationDate Desc",
                      new { PerAppSummaryCount }, commandTimeout: QueryTimeout).ToList())
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
        public List<Error> GetAllErrors(int maxPerApp, string appName = null)
        {
            try
            {
                using (MiniProfiler.Current.Step("GetAllErrors() for " + Name + " App: " + (appName ?? "All")))
                using (var c = GetConnection())
                {
                    var sql = @"
Select e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.StatusCode, e.ErrorHash, e.DuplicateCount
  From (Select Id, Rank() Over (Partition By ApplicationName Order By CreationDate desc) as r
		From Exceptions
		Where DeletionDate Is Null" + (appName.HasValue() ? " And ApplicationName = @appName" : "") + @") er
	   Inner Join Exceptions e On er.Id = e.Id
 Where er.r <= @maxPerApp
 Order By CreationDate Desc";
                    return c.Query<Error>(sql, new {maxPerApp, appName}, commandTimeout: QueryTimeout).ToList();
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return new List<Error>();
            }
        }

        public List<Error> GetSimilarErrors(Error error, int max)
        {
            try
            {
                using (MiniProfiler.Current.Step("GetSimilarErrors() for " + Name))
                using (var c = GetConnection())
                {
                    var result = c.Query<Error>(@"
    Select Top (@max) e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where ApplicationName = @ApplicationName
       And Message = @Message
     Order By CreationDate Desc", new { max, error.ApplicationName, error.Message }, commandTimeout: QueryTimeout).ToList();
                    return result;
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return new List<Error>();
            }
        }

        public List<Error> GetSimilarErrorsInTime(Error error, int max)
        {
            try
            {
                using (MiniProfiler.Current.Step("GetSimilarErrorsInTime() for " + Name))
                using (var c = GetConnection())
                {
                    var result = c.Query<Error>(@"
    Select Top (@max) e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where CreationDate Between @start and @end
     Order By CreationDate Desc", new { max, start = error.CreationDate.AddMinutes(-5), end = error.CreationDate.AddMinutes(5) }, commandTimeout: QueryTimeout).ToList();
                    return result;
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return new List<Error>();
            }
        }

        public List<Error> FindErrors(string searchText, string appName, int max, bool includeDeleted)
        {
            try
            {
                var excludeSearchText = searchText.StartsWith("-");
                if (excludeSearchText)
                {
                    searchText = searchText.Substring(1);
                }

                using (MiniProfiler.Current.Step("FindErrors() for " + Name))
                using (var c = GetConnection())
                {
                    var result = c.Query<Error>(@"
    Select Top (@max) e.Id, e.GUID, e.ApplicationName, e.MachineName, e.CreationDate, e.Type, e.IsProtected, e.Host, e.Url, e.HTTPMethod, e.IPAddress, e.Source, e.Message, e.Detail, e.StatusCode, e.ErrorHash, e.DuplicateCount, e.DeletionDate
      From Exceptions e
     Where (" + (excludeSearchText ? @"Message Not Like @search And Detail Not Like @search And Url Not Like @search" : @"Message Like @search Or Detail Like @search Or Url Like @search") + @")" + (appName.HasValue() ? " And ApplicationName = @appName" : "") + (includeDeleted ? "" : " And DeletionDate Is Null") + @"
     Order By CreationDate Desc", new { search = '%' + searchText + '%', appName, max }, commandTimeout: QueryTimeout).ToList();
                    return result;
                }
            }
            catch (Exception e)
            {
                Current.LogException(e);
                return new List<Error>();
            }
        }

        public int DeleteAllErrors(string appName)
        {
            using (MiniProfiler.Current.Step("DeleteAllErrors() (app: " + appName + ") for " + Name))
            using (var c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @appName", new { appName }, commandTimeout: QueryTimeout);
            }
        }

        public int DeleteSimilarErrors(Error error)
        {
            using (MiniProfiler.Current.Step("DeleteSimilarErrors('" + error.GUID + "') (app: " + error.ApplicationName + ") for " + Name))
            using (var c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where ApplicationName = @ApplicationName
   And Message = @Message
   And DeletionDate Is Null
   And IsProtected = 0", new { error.ApplicationName, error.Message }, commandTimeout: QueryTimeout);
            }
        }

        public int DeleteErrors(string appName, List<Guid> ids)
        {
            using (MiniProfiler.Current.Step("DeleteErrors(" + ids.Count + " Guids) (app: " + appName + ") for " + Name))
            using (var c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where DeletionDate Is Null 
   And IsProtected = 0 
   And ApplicationName = @appName
   And GUID In @ids", new { appName, ids }, commandTimeout: QueryTimeout);
            }
        }
        
        public Error GetError(Guid guid)
        {
            try
            {
                Error sqlError;
                using (MiniProfiler.Current.Step("GetError() (guid: " + guid + ") for " + Name))
                using (var c = GetConnection())
                {
                    sqlError = c.Query<Error>(@"
    Select * 
      From Exceptions 
     Where GUID = @guid", new { guid }, commandTimeout: QueryTimeout).FirstOrDefault();
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

        public bool ProtectError(Guid guid)
        {
            using (MiniProfiler.Current.Step("ProtectError() (guid: " + guid + ") for " + Name))
            using (var c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set IsProtected = 1, DeletionDate = Null
 Where GUID = @guid", new { guid }, commandTimeout: QueryTimeout) > 0;
            }
        }

        public bool DeleteError(Guid guid)
        {
            using (MiniProfiler.Current.Step("DeleteError() (guid: " + guid + ") for " + Name))
            using (var c = GetConnection())
            {
                return c.Execute(@"
Update Exceptions 
   Set DeletionDate = GETUTCDATE() 
 Where GUID = @guid 
   And DeletionDate Is Null", new { guid }, commandTimeout: QueryTimeout) > 0;
            }
        }

        private DbConnection GetConnection()
        {
            return Connection.GetOpen(Settings.ConnectionString, QueryTimeout);
        }
    }
}