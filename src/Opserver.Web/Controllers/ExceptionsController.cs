using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data.Exceptions;
using Opserver.Data.Jira;
using Opserver.Helpers;
using Opserver.Models;
using Opserver.Views.Exceptions;
using StackExchange.Exceptional;

namespace Opserver.Controllers
{
    [OnlyAllow(Roles.Exceptions)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class ExceptionsController : StatusController<ExceptionsModule>
    {
        public const int MaxSearchResults = 2000;
        private List<ApplicationGroup> ApplicationGroups => CurrentStore.ApplicationGroups;
        private readonly ExceptionStore CurrentStore;
        private readonly string CurrentGroup;
        private readonly string CurrentLog;
        private Guid? CurrentId;
        private Guid? CurrentSimilarId;
        private readonly ExceptionSorts CurrentSort;

        public ExceptionsController(ExceptionsModule module, IOptions<OpserverSettings> settings) : base(module, settings)
        {
            CurrentStore = Module.GetStore(GetParam("store"));
            CurrentGroup = GetParam("group");
            CurrentLog = GetParam("log") ?? GetParam("app"); // old link compat
            CurrentId = GetParam("id").HasValue() && Guid.TryParse(GetParam("id"), out var guid) ? guid : (Guid?)null;
            CurrentSimilarId = GetParam("similar").HasValue() && Guid.TryParse(GetParam("similar"), out var similarGuid) ? similarGuid : (Guid?)null;
            Enum.TryParse(GetParam("sort"), out CurrentSort);

            if (CurrentLog.HasValue())
            {
                var storeApps = CurrentStore.Applications.Data;
                var a = storeApps?.Find(app => app.Name == CurrentLog) ?? storeApps?.Find(app => app.ShortName == CurrentLog);
                if (a != null)
                {
                    // Correct the log name to a found one, this enables short names to work.
                    CurrentLog = a.Name;
                    // Make pre-group links work correctly
                    if (CurrentGroup.IsNullOrEmpty())
                    {
                        // Old links, that didn't know about groups
                        var g = ApplicationGroups.Find(gr => gr[a.Name] != null);
                        if (g != null)
                        {
                            CurrentGroup = g.Name;
                        }
                    }
                }
            }
        }

        public string GetParam(string param) => Current.Request.HasFormContentType ? Current.Request.Form[param] : Current.Request.Query[param];

        // TODO: Move entirely to model binder
        private async Task<ExceptionStore.SearchParams> GetSearchAsync()
        {
            var result = new ExceptionStore.SearchParams
            {
                Group = CurrentGroup,
                Log = CurrentLog,
                Sort = CurrentSort,
                Id = CurrentId
            };

            if (GetParam("q").HasValue())
            {
                result.SearchQuery = GetParam("q");
            }
            if (bool.TryParse(GetParam("includeDeleted"), out var includeDeleted))
            {
                result.IncludeDeleted = includeDeleted;
            }

            if (CurrentSimilarId.HasValue)
            {
                var error = await CurrentStore.GetErrorAsync(CurrentLog, CurrentSimilarId.Value);
                if (error != null)
                {
                    result.Message = error.Message;
                }
            }

            return result;
        }

        private ExceptionsModel GetModel(List<Error> errors)
        {
            var group = CurrentGroup.HasValue() ? CurrentStore.ApplicationGroups.Find(g => g.Name == CurrentGroup) : null;
            var log = group != null && CurrentLog.HasValue() ? group.Applications.Find(a => a.Name == CurrentLog) : null;
            // Handle single-log groups a bit more intuitively so things like clear all work
            if (log == null && group?.Applications.Count == 1)
            {
                log = group.Applications[0];
            }
            return new ExceptionsModel
            {
                Module = Module,
                Store = CurrentStore,
                Groups = ApplicationGroups,
                Group = group,
                Log = log,
                Sort = CurrentSort,
                Errors = errors
            };
        }

        [DefaultRoute("exceptions")]
        public async Task<ActionResult> Exceptions()
        {
            var search = await GetSearchAsync();
            var errors = await CurrentStore.GetErrorsAsync(search);

            var vd = GetModel(errors);
            vd.SearchParams = search;
            vd.LoadAsyncSize = Module.Settings.PageSize;
            return View(vd);
        }

        [Route("exceptions/load-more")]
        public async Task<ActionResult> LoadMore(int? count = null, Guid? prevLast = null)
        {
            var search = await GetSearchAsync();
            search.Count = count ?? Module.Settings.PageSize;
            search.StartAt = prevLast;

            var errors = await CurrentStore.GetErrorsAsync(search);
            var vd = GetModel(errors);
            vd.SearchParams = search;
            return View("Exceptions.Table.Rows", vd);
        }

        [Route("exceptions/detail")]
        public async Task<ActionResult> Detail(Guid id)
        {
            var e = await CurrentStore.GetErrorAsync(CurrentLog, id);
            var vd = GetModel(null);
            vd.Exception = e;
            return View("Exceptions.Detail", vd);
        }

        [Route("exceptions/preview")]
        public async Task<ActionResult> Preview(Guid id)
        {
            var e = await CurrentStore.GetErrorAsync(CurrentLog, id);

            var vd = GetModel(null);
            vd.Exception = e;
            return PartialView("Exceptions.Preview", vd);
        }

        [AllowAnonymous]
        [Route("exceptions/detail/json")]
        public async Task<ActionResult> DetailJson(Guid id)
        {
            var e = await CurrentStore.GetErrorAsync(CurrentLog, id);
            if (e == null)
            {
                return JsonNotFound();
            }
            else
            {
                return Json(new
                {
                    e.GUID,
                    e.ErrorHash,
                    e.ApplicationName,
                    e.Type,
                    e.Source,
                    e.Message,
                    e.Detail,
                    e.MachineName,
                    e.Host,
                    e.FullUrl,
                    e.HTTPMethod,
                    e.IPAddress,
                    e.DuplicateCount,
                    CreationDate = e.CreationDate.ToEpochTime(),
                    e.Commands,
                });
            }
        }

        [Route("exceptions/protect"), HttpPost, OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> Protect(Guid id, bool redirect = false)
        {
            var success = await CurrentStore.ProtectErrorAsync(id);
            if (!success) JsonError("Unable to protect, error was not found in the log");
            return redirect ? Json(new { url = Url.Action(nameof(Exceptions), new { store = CurrentStore.Name, group = CurrentGroup, log = CurrentLog }) }) : Counts();
        }

        [Route("exceptions/delete"), HttpPost, OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> Delete()
        {
            var toDelete = await GetSearchAsync();
            // we don't care about success...if it's *already* deleted, that's fine
            // if we throw an exception trying to delete, that's another matter
            if (toDelete.Id.HasValue)
            {
                // optimized single route
                await CurrentStore.DeleteErrorAsync(toDelete.Id.Value);
            }
            else
            {
                await CurrentStore.DeleteErrorsAsync(toDelete);
            }

            return toDelete.Id.HasValue
                ? Counts()
                : Json(new { url = Url.Action(nameof(Exceptions), new { store = CurrentStore.Name, group = CurrentGroup, log = CurrentLog }) });
        }

        [Route("exceptions/delete-list"), HttpPost, OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> DeleteList(Guid[] ids, bool returnCounts = false)
        {
            if (ids == null || ids.Length == 0) return Json(true);
            await CurrentStore.DeleteErrorsAsync(ids.ToList());

            return returnCounts ? Counts() : Json(new { url = Url.Action("Exceptions", new { store = CurrentStore.Name, log = CurrentLog, group = CurrentGroup }) });
        }

        [Route("exceptions/counts")]
        public ActionResult Counts()
        {
            var stores = Module.Stores.Select(s => new
            {
                s.Name,
                Total = s.TotalExceptionCount
            });
            var groups = ApplicationGroups.Select(g => new
            {
                g.Name,
                g.Total,
                Applications = g.Applications.Select(a => new
                {
                    a.Name,
                    Total = a.ExceptionCount
                })
            });
            return Json(new
            {
                Stores = stores,
                Groups = groups,
                Total = Module.TotalExceptionCount
            });
        }

        [Route("exceptions/jiraactions"), HttpGet, OnlyAllow(Roles.ExceptionsAdmin)]
        public ActionResult JiraActions(string appName)
        {
            var issues = Module.Settings.Jira.GetActionsForApplication(appName);
            return View("Exceptions.Jira", issues);
        }

        [Route("exceptions/jiraaction"), HttpGet, OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> JiraAction(Guid id, int actionid)
        {
            var e = await CurrentStore.GetErrorAsync(CurrentLog, id);
            var user = Current.User;
            var action = Module.Settings.Jira.Actions.Find(i => i.Id == actionid);
            var jiraClient = new JiraClient(Module.Settings.Jira);
            var result = await jiraClient.CreateIssueAsync(action, e, user == null ? "" : user.AccountName);

            if (string.IsNullOrWhiteSpace(result.Key))
            {
                return Json(new
                {
                    success = false,
                    message = "Can not create issue"
                });
            }

            return Json(new
            {
                success = true,
                issueKey = result.Key,
                browseUrl = result.BrowseUrl
            });
        }
    }
}
