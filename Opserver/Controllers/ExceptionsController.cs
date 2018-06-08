using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Exceptions;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Exceptions;
using System.Threading.Tasks;
using StackExchange.Opserver.Data.Jira;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Exceptions)]
    public class ExceptionsController : StatusController
    {
        public const int MaxSearchResults = 2000;

        public override ISecurableModule SettingsModule => Current.Settings.Exceptions;

        public override TopTab TopTab => new TopTab("Exceptions", nameof(Exceptions), this, 50)
        {
            GetMonitorStatus = () => ExceptionsModule.MonitorStatus,
            GetBadgeCount = () => ExceptionsModule.TotalExceptionCount,
            GetTooltip = () => ExceptionsModule.TotalRecentExceptionCount.ToComma() + " recent"
        };

        private List<ApplicationGroup> ApplicationGroups => CurrentStore.ApplicationGroups;
        private ExceptionStore CurrentStore;
        private string CurrentGroup;
        private string CurrentLog;
        private Guid? CurrentId;
        private Guid? CurrentSimilarId;
        private ExceptionSorts CurrentSort;

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            CurrentStore = ExceptionsModule.GetStore(Request.Params["store"]);
            CurrentGroup = Request.Params["group"];
            CurrentLog = Request.Params["log"] ?? Request.Params["app"]; // old link compat
            CurrentId = Request.Params["id"].HasValue() && Guid.TryParse(Request.Params["id"], out var guid) ? guid : (Guid?)null;
            CurrentSimilarId = Request.Params["similar"].HasValue() && Guid.TryParse(Request.Params["similar"], out var similarGuid) ? similarGuid : (Guid?)null;
            Enum.TryParse(Request.Params["sort"], out CurrentSort);

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

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            base.OnActionExecuting(filterContext);
        }

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

            if (Request.Params["q"].HasValue())
            {
                result.SearchQuery = Request.Params["q"];
            }
            if (bool.TryParse(Request.Params["includeDeleted"], out var includeDeleted))
            {
                result.IncludeDeleted = includeDeleted;
            }

            if (CurrentSimilarId.HasValue)
            {
                var error = await CurrentStore.GetErrorAsync(CurrentLog, CurrentSimilarId.Value).ConfigureAwait(false);
                if (error != null)
                {
                    result.Message = error.Message;
                }
            }

            return result;
        }

        private ExceptionsModel GetModel(List<Exceptional.Error> errors)
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
                Store = CurrentStore,
                Groups = ApplicationGroups,
                Group = group,
                Log = log,
                Sort = CurrentSort,
                Errors = errors
            };
        }

        [Route("exceptions")]
        public async Task<ActionResult> Exceptions()
        {
            var search = await GetSearchAsync().ConfigureAwait(false);
            var errors = await CurrentStore.GetErrorsAsync(search).ConfigureAwait(false);

            var vd = GetModel(errors);
            vd.SearchParams = search;
            vd.LoadAsyncSize = Current.Settings.Exceptions.PageSize;
            return View(vd);
        }

        [Route("exceptions/load-more")]
        public async Task<ActionResult> LoadMore(int? count = null, Guid? prevLast = null)
        {
            var search = await GetSearchAsync().ConfigureAwait(false);
            search.Count = count ?? Current.Settings.Exceptions.PageSize;
            search.StartAt = prevLast;

            var errors = await CurrentStore.GetErrorsAsync(search).ConfigureAwait(false);
            var vd = GetModel(errors);
            vd.SearchParams = search;
            return View("Exceptions.Table.Rows", vd);
        }

        [Route("exceptions/detail")]
        public async Task<ActionResult> Detail(Guid id)
        {
            var e = await CurrentStore.GetErrorAsync(CurrentLog, id).ConfigureAwait(false);
            var vd = GetModel(null);
            vd.Exception = e;
            return View("Exceptions.Detail", vd);
        }

        [Route("exceptions/preview")]
        public async Task<ActionResult> Preview(Guid id)
        {
            var e = await CurrentStore.GetErrorAsync(CurrentLog, id).ConfigureAwait(false);

            var vd = GetModel(null);
            vd.Exception = e;
            return PartialView("Exceptions.Preview", vd);
        }

        [Route("exceptions/detail/json"), AlsoAllow(Roles.Anonymous)]
        public async Task<JsonResult> DetailJson(Guid id)
        {
            var e = await CurrentStore.GetErrorAsync(CurrentLog, id).ConfigureAwait(false);
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

        [Route("exceptions/protect"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> Protect(Guid id, bool redirect = false)
        {
            var success = await CurrentStore.ProtectErrorAsync(id).ConfigureAwait(false);
            if (!success) JsonError("Unable to protect, error was not found in the log");
            return redirect ? Json(new { url = Url.Action(nameof(Exceptions), new { store = CurrentStore.Name, group = CurrentGroup, log = CurrentLog }) }) : Counts();
        }

        [Route("exceptions/delete"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> Delete()
        {
            var toDelete = await GetSearchAsync().ConfigureAwait(false);
            // we don't care about success...if it's *already* deleted, that's fine
            // if we throw an exception trying to delete, that's another matter
            if (toDelete.Id.HasValue)
            {
                // optimized single route
                await CurrentStore.DeleteErrorAsync(toDelete.Id.Value).ConfigureAwait(false);
            }
            else
            {
                await CurrentStore.DeleteErrorsAsync(toDelete).ConfigureAwait(false);
            }

            return toDelete.Id.HasValue
                ? Counts()
                : Json(new { url = Url.Action(nameof(Exceptions), new { store = CurrentStore.Name, group = CurrentGroup, log = CurrentLog }) });
        }

        [Route("exceptions/delete-list"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> DeleteList(Guid[] ids, bool returnCounts = false)
        {
            if (ids == null || ids.Length == 0) return Json(true);
            await CurrentStore.DeleteErrorsAsync(ids.ToList()).ConfigureAwait(false);

            return returnCounts ? Counts() : Json(new { url = Url.Action("Exceptions", new { store = CurrentStore.Name, log = CurrentLog, group = CurrentGroup }) });
        }

        [Route("exceptions/counts")]
        public ActionResult Counts()
        {
            var stores = ExceptionsModule.Stores.Select(s => new
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
                Total = ExceptionsModule.TotalExceptionCount
            });
        }

        [Route("exceptions/jiraactions"), AcceptVerbs(HttpVerbs.Get), OnlyAllow(Roles.ExceptionsAdmin)]
        public ActionResult JiraActions(string appName)
        {
            var issues = Current.Settings.Jira.GetActionsForApplication(appName);
            return View("Exceptions.Jira", issues);
        }

        [Route("exceptions/jiraaction"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> JiraAction(Guid id, int actionid)
        {
            var e = await CurrentStore.GetErrorAsync(CurrentLog, id).ConfigureAwait(false);
            var user = Current.User;
            var action = Current.Settings.Jira.Actions.Find(i => i.Id == actionid);
            var jiraClient = new JiraClient(Current.Settings.Jira);
            var result = await jiraClient.CreateIssueAsync(action, e, user == null ? "" : user.AccountName).ConfigureAwait(false);

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
