﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Profiling;
using StackExchange.Exceptional;
using Opserver.Helpers;
using System.Globalization;

namespace Opserver.Data.Exceptions
{
    public class ExceptionStore : PollNode<ExceptionsModule>
    {
        public const int PerAppSummaryCount = 1000;

        public override string ToString() => "Store: " + Settings.Name;

        private int? QueryTimeout => Settings.QueryTimeoutMs;
        public string Name => Settings.Name;
        public string Description => Settings.Description;
        public string TableName => Settings.TableName.IsNullOrEmptyReturn("Exceptions");
        public ExceptionsSettings.Store Settings { get; internal set; }

        public override int MinSecondsBetweenPolls => 1;
        public override string NodeType => "Exceptions";

        public override IEnumerable<Cache> DataPollers
        {
            get { yield return Applications; }
        }

        protected override IEnumerable<MonitorStatus> GetMonitorStatus()
        {
            yield return DataPollers.GetWorstStatus();
        }

        protected override string GetMonitorStatusReason() => null;

        public ExceptionStore(ExceptionsModule module, ExceptionsSettings.Store settings) : base(module, settings.Name)
        {
            Settings = settings;
            ApplicationGroups = GetConfiguredApplicationGroups();
            KnownApplications = ApplicationGroups.SelectMany(g => g.Applications.Select(a => a.Name)).ToHashSet();
        }

        public int TotalExceptionCount => Applications.Data?.Sum(a => a.ExceptionCount) ?? 0;
        public int TotalRecentExceptionCount => Applications.Data?.Sum(a => a.RecentExceptionCount) ?? 0;
        private ApplicationGroup CatchAll { get; set; }
        public List<ApplicationGroup> ApplicationGroups { get; private set; }
        public HashSet<string> KnownApplications { get; }

        private List<ApplicationGroup> GetConfiguredApplicationGroups()
        {
            var groups = Settings.Groups ?? Module.Settings.Groups;
            var applications = Settings.Applications ?? Module.Settings.Applications;
            if (groups?.Any() ?? false)
            {
                var configured = groups
                    .Select(g => new ApplicationGroup
                    {
                        Name = g.Name,
                        Applications = g.Applications.OrderBy(a => a).Select(a => new Application { Name = a }).ToList()
                    }).ToList();
                // The user could have configured an "Other", don't duplicate it
                if (configured.All(g => g.Name != "Other"))
                {
                    configured.Add(new ApplicationGroup { Name = "Other" });
                }
                CatchAll = configured.First(g => g.Name == "Other");
                return configured;
            }
            // One big bucket if nothing is configured
            CatchAll = new ApplicationGroup { Name = "All" };
            if (applications?.Any() ?? false)
            {
                CatchAll.Applications = applications.OrderBy(a => a).Select(a => new Application { Name = a }).ToList();
            }
            return new List<ApplicationGroup>
            {
                CatchAll
            };
        }

        private Cache<List<Application>> _applications;
        public Cache<List<Application>> Applications =>
            _applications ??= new Cache<List<Application>>(
                this,
                "Exceptions Fetch: " + Name + ":" + nameof(Applications),
                Settings.PollIntervalSeconds.Seconds(),
                async () =>
                {
                    var result = await QueryListAsync<Application>($"Applications Fetch: {Name}", @"
Select ApplicationName as Name,
       Sum(DuplicateCount) as ExceptionCount,
	   Sum(Case When CreationDate > DateAdd(Second, -@RecentSeconds, GETUTCDATE()) Then DuplicateCount Else 0 End) as RecentExceptionCount,
	   MAX(CreationDate) as MostRecent
  From Exceptions
 Where DeletionDate Is Null
 Group By ApplicationName", new { Module.Settings.RecentSeconds });
                    result.ForEach(a =>
                    {
                        a.StoreName = Name;
                        a.Store = this;
                    });
                    return result;
                },
                afterPoll: _ => UpdateApplicationGroups());

        internal List<ApplicationGroup> UpdateApplicationGroups()
        {
            using (MiniProfiler.Current.Step(nameof(UpdateApplicationGroups)))
            {
                var result = ApplicationGroups;
                var apps = Applications.Data;
                // Loop through all configured groups and hook up applications returned from the queries
                foreach (var g in result)
                {
                    for (var i = 0; i < g.Applications.Count; i++)
                    {
                        var a = g.Applications[i];
                        foreach (var app in apps)
                        {
                            if (app.Name == a.Name)
                            {
                                g.Applications[i] = app;
                                break;
                            }
                        }
                    }
                }
                // Clear all dynamic apps from the CatchAll group unless we found them again (minimal delta)
                // Doing this atomically to minimize enumeration conflicts
                CatchAll.Applications.RemoveAll(a => !apps.Any(ap => ap.Name == a.Name) && !KnownApplications.Contains(a.Name));

                // Check for the any dyanmic/unconfigured apps that we need to add to the all group
                foreach (var app in apps)
                {
                    if (!KnownApplications.Contains(app.Name) && !CatchAll.Applications.Any(a => a.Name == app.Name))
                    {
                        // This dynamic app needs a home!
                        CatchAll.Applications.Add(app);
                    }
                }
                CatchAll.Applications.Sort((a, b) => a.Name.CompareTo(b.Name));

                // Cleanout those with no errors right now
                var foundNames = apps.Select(a => a.Name).ToHashSet();
                foreach (var a in result.SelectMany(g => g.Applications))
                {
                    if (!foundNames.Contains(a.Name))
                    {
                        a.ClearCounts();
                    }
                }

                ApplicationGroups = result;
                return result;
            }
        }

        public class SearchParams
        {
            public string Group { get; set; }
            public string Log { get; set; }
            public int Count { get; set; } = 250;
            public bool IncludeDeleted { get; set; }
            public string SearchQuery { get; set; }
            public string Message { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public Guid? StartAt { get; set; }
            public ExceptionSorts Sort { get; set; }
            public Guid? Id { get; set; }
            public string Url { get; set; }
            public string Host { get; set; }

            public bool HasFilterOptions =>
              Host.HasValue() || Url.HasValue() || StartDate.HasValue || EndDate.HasValue;

            public string NormalizedStartDate => NormalizeDateTime(StartDate);

            public string NormalizedEndDate => NormalizeDateTime(EndDate);

            public override int GetHashCode()
            {
                var hashCode = -1510201480;
                hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Group);
                hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Log);
                hashCode = (hashCode * -1521134295) + Count.GetHashCode();
                hashCode = (hashCode * -1521134295) + IncludeDeleted.GetHashCode();
                hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(SearchQuery);
                hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Message);
                hashCode = (hashCode * -1521134295) + EqualityComparer<DateTime?>.Default.GetHashCode(StartDate);
                hashCode = (hashCode * -1521134295) + EqualityComparer<DateTime?>.Default.GetHashCode(EndDate);
                hashCode = (hashCode * -1521134295) + EqualityComparer<Guid?>.Default.GetHashCode(StartAt);
                hashCode = (hashCode * -1521134295) + EqualityComparer<Guid?>.Default.GetHashCode(Id);
                hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Url);
                hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Host);
                return (hashCode * -1521134295) + Sort.GetHashCode();
            }

            private string NormalizeDateTime(DateTime? dateTime)
            {
                // To keep things consistent we normalize the string representation of datetimes to a single format. The controller will receive the same date format through GET, POST and ajax calls, no matter what the user types in.
                // The chosen format is the same as the html datetime picker uses to format the value the user enters, we use the same one to avoid unnecessary conversions.
                // https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/datetime-local
                // The format the user sees will always depend on the user's browser preferences, but the datepicker will convert it automatically.
                return dateTime?.ToString("yyyy'-'MM'-'dd'T'HH':'mm", CultureInfo.InvariantCulture);
            }
        }

        public Task<List<Error>> GetErrorsAsync(SearchParams search)
        {
            var query = GetSearchQuery(search, QueryMode.Search);
            return QueryListAsync<Error>($"{nameof(GetErrorsAsync)}() for {Name}", query.SQL, query.Params);
        }

        private enum QueryMode { Search, Delete }

        // TODO: Move this into a SQL-specific provider maybe, something swappable
        private (string SQL, object Params) GetSearchQuery(SearchParams search, QueryMode mode)
        {
            var sb = StringBuilderCache.Get();
            bool firstWhere = true;
            void AddClause(string clause)
            {
                sb.Append(firstWhere ? " Where " : "   And ").AppendLine(clause);
                firstWhere = false;
            }

            sb.Append(@"With list As (
Select e.Id,
	   e.GUID,
	   e.ApplicationName,
	   e.MachineName,
	   e.CreationDate,
	   e.Type,
	   e.IsProtected,
	   e.Host,
	   e.Url,
	   e.HTTPMethod,
	   e.IPAddress,
	   e.Source,
	   e.Message,
	   e.StatusCode,
	   e.ErrorHash,
	   e.DuplicateCount,
	   e.DeletionDate,
	   ROW_NUMBER() Over(").Append(GetSortString(search.Sort)).Append(@") rowNum
  From ").Append(TableName).AppendLine(" e");

            var logs = GetAppNames(search);
            if (search.Log.HasValue() || search.Group.HasValue())
            {
                AddClause("ApplicationName In @logs");
            }
            if (search.Message.HasValue())
            {
                AddClause("Message = @Message");
            }
            if (!search.IncludeDeleted)
            {
                AddClause("DeletionDate Is Null");
            }
            if (search.StartDate.HasValue)
            {
                AddClause("CreationDate >= @StartDate");
            }
            if (search.EndDate.HasValue)
            {
                AddClause("CreationDate <= @EndDate");
            }
            if (search.SearchQuery.HasValue())
            {
                AddClause("(Message Like @query Or Url Like @query)");
            }
            if (search.Id.HasValue)
            {
                AddClause("Id = @Id");
            }
            if (search.Url.HasValue())
            {
                AddClause("Url Like @Url");
            }
            if (search.Host.HasValue())
            {
                AddClause("Host Like @Host");
            }
            if (mode == QueryMode.Delete)
            {
                AddClause("IsProtected = 0");
            }

            sb.AppendLine(")");
            switch (mode)
            {
                case QueryMode.Search:
                    sb.AppendLine("  Select Top {=Count} *");
                    break;
                case QueryMode.Delete:
                    sb.AppendLine("  Update list");
                    sb.AppendLine("    Set DeletionDate = GetUtcDate(), IsProtected = 0");
                    break;
            }
            sb.AppendLine("    From list");
            if (search.StartAt.HasValue)
            {
                sb.AppendLine("   Where rowNum > (Select Top 1 rowNum From list Where GUID = @StartAt)");
            }
            if (mode == QueryMode.Search)
            {
                sb.AppendLine("Order By rowNum");
            }

            return (sb.ToStringRecycle(), new
            {
                logs,
                search.Message,
                search.StartDate,
                search.EndDate,
                query = "%" + search.SearchQuery + "%",
                search.StartAt,
                search.Count,
                search.Id,
                Url = search.Url?.Replace('*', '%'),
                Host = search.Host?.Replace('*', '%')
            });
        }

        public IEnumerable<string> GetAppNames(SearchParams search) => GetAppNames(search.Group, search.Log);
        public IEnumerable<string> GetAppNames(string group, string app)
        {
            if (app.HasValue())
            {
                yield return app;
                yield break;
            }
            if (group.HasValue())
            {
                var apps = ApplicationGroups?.FirstOrDefault(g => g.Name == group)?.Applications;
                if (apps != null)
                {
                    foreach (var a in apps)
                    {
                        yield return a.Name;
                    }
                }
            }
        }

        private static string GetSortString(ExceptionSorts sort) =>
            sort switch
            {
                ExceptionSorts.AppAsc => " Order By ApplicationName, CreationDate Desc",
                ExceptionSorts.AppDesc => " Order By ApplicationName Desc, CreationDate Desc",
                ExceptionSorts.TypeAsc => " Order By Right(e.Type, charindex('.', reverse(e.Type) + '.') - 1), CreationDate Desc",
                ExceptionSorts.TypeDesc => " Order By Right(e.Type, charindex('.', reverse(e.Type) + '.') - 1) Desc, CreationDate Desc",
                ExceptionSorts.MessageAsc => " Order By Message, CreationDate Desc",
                ExceptionSorts.MessageDesc => " Order By Message Desc, CreationDate Desc",
                ExceptionSorts.UrlAsc => " Order By Url, CreationDate Desc",
                ExceptionSorts.UrlDesc => " Order By Url Desc, CreationDate Desc",
                ExceptionSorts.IPAddressAsc => " Order By IPAddress, CreationDate Desc",
                ExceptionSorts.IPAddressDesc => " Order By IPAddress Desc, CreationDate Desc",
                ExceptionSorts.HostAsc => " Order By Host, CreationDate Desc",
                ExceptionSorts.HostDesc => " Order By Host Desc, CreationDate Desc",
                ExceptionSorts.MachineNameAsc => " Order By MachineName, CreationDate Desc",
                ExceptionSorts.MachineNameDesc => " Order By MachineName Desc, CreationDate Desc",
                ExceptionSorts.CountAsc => " Order By IsNull(DuplicateCount, 1), CreationDate Desc",
                ExceptionSorts.CountDesc => " Order By IsNull(DuplicateCount, 1) Desc, CreationDate Desc",
                ExceptionSorts.TimeAsc => " Order By CreationDate",
                //case ExceptionSorts.TimeDesc:
                _ => " Order By CreationDate Desc",
            };

        public Task<int> DeleteErrorsAsync(SearchParams search)
        {
            var query = GetSearchQuery(search, QueryMode.Delete);
            return ExecTaskAsync($"{nameof(DeleteErrorsAsync)}() for {Name}", query.SQL, query.Params);
        }

        public Task<int> DeleteErrorsAsync(List<Guid> ids)
        {
            return ExecTaskAsync($"{nameof(DeleteErrorsAsync)}({ids.Count} Guids) for {Name}", @"
Update Exceptions
   Set DeletionDate = GETUTCDATE()
 Where DeletionDate Is Null
   And IsProtected = 0
   And GUID In @ids", new { ids });
        }

        public async Task<Error> GetErrorAsync(string _, Guid guid)
        {
            try
            {
                Error sqlError;
                using (MiniProfiler.Current.Step(nameof(GetErrorAsync) + "() (guid: " + guid.ToString() + ") for " + Name))
                using (var c = await GetConnectionAsync())
                {
                    sqlError = await c.QueryFirstOrDefaultAsync<Error>(@"
    Select Top 1 *
      From Exceptions
     Where GUID = @guid", new { guid }, commandTimeout: QueryTimeout);
                }
                if (sqlError == null) return null;

                // everything is in the JSON, but not the columns and we have to deserialize for collections anyway
                // so use that deserialized version and just get the properties that might change on the SQL side and apply them
                var result = Error.FromJson(sqlError.FullJson);
                result.DuplicateCount = sqlError.DuplicateCount;
                result.DeletionDate = sqlError.DeletionDate;
                result.ApplicationName = sqlError.ApplicationName;
                result.IsProtected = sqlError.IsProtected;
                return result;
            }
            catch (Exception e)
            {
                e.Log();
                return null;
            }
        }

        public async Task<bool> ProtectErrorAsync(Guid guid)
        {
              return await ExecTaskAsync($"{nameof(ProtectErrorAsync)}() (guid: {guid}) for {Name}", @"
Update Exceptions
   Set IsProtected = 1, DeletionDate = Null
 Where GUID = @guid", new {guid}) > 0;
        }

        public async Task<bool> DeleteErrorAsync(Guid guid)
        {
            return await ExecTaskAsync($"{nameof(DeleteErrorAsync)}() (guid: {guid}) for {Name}", @"
Update Exceptions
   Set DeletionDate = GETUTCDATE()
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
                e.Log();
                return new List<T>();
            }
        }

        public async Task<int> ExecTaskAsync(string step, string sql, dynamic paramsObj)
        {
            using (MiniProfiler.Current.Step(step))
            using (var c = await GetConnectionAsync())
            {
                // Perform the action
                var result = await c.ExecuteAsync(sql, paramsObj as object, commandTimeout: QueryTimeout);
                // Refresh our caches
                await Applications.PollAsync(!Applications.IsPolling);
                return result;
            }
        }

        private Task<DbConnection> GetConnectionAsync() =>
            Connection.GetOpenAsync(Settings.ConnectionString, QueryTimeout);
    }
}
