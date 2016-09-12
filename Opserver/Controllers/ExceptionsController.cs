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
using Microsoft.Ajax.Utilities;
using StackExchange.Opserver.Data.Jira;
using static StackExchange.Opserver.Data.Exceptions.ExceptionStores;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Exceptions)] 
    public class ExceptionsController : StatusController
    {
        public override ISecurableSection SettingsSection => Current.Settings.Exceptions;

        public override TopTab TopTab => new TopTab("Exceptions", nameof(Exceptions), this, 50)
        {
            GetMonitorStatus = () => MonitorStatus,
            GetBadgeCount = () => TotalExceptionCount,
            GetTooltip = () => TotalRecentExceptionCount.ToComma() + " recent"
        };

        private JiraSettings JiraSettings => Current.Settings.Jira;

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            base.OnActionExecuting(filterContext); 
        }

        private void FixNames(ref string group, ref string log)
        {
            if (log.HasValue())
            {
                var dLog = log;
                var a = Applications.FirstOrDefault(app => app.Name == dLog) ?? Applications.FirstOrDefault(app => app.ShortName == dLog);
                log = a != null ? a.Name : log;
                if (a != null && group.IsNullOrEmpty())
                {
                    // Old links, that didn't know about groups
                    var g = ApplicationGroups.FirstOrDefault(gr => gr[a.Name] != null);
                    if (g != null) group = g.Name;
                }
            }
        }

        [Route("exceptions")]
        public ActionResult Exceptions(string group, string log, ExceptionSorts? sort = null, int? count = null)
        {
            // Defaults
            count = count ?? 250;
            sort = sort ?? ExceptionSorts.TimeDesc;

            var vd = GetExceptionsModel(group, log, sort.Value, count.Value, loadAsync: 500);
            return View(vd);
        }

        [Route("exceptions/load-more")]
        public ActionResult LoadMore(string group, string log, ExceptionSorts sort, int? count = null, Guid? prevLast = null)
        {
            var vd = GetExceptionsModel(group, log, sort, count, prevLast);
            return View("Exceptions.Table.Rows", vd);
        }

        public ExceptionsModel GetExceptionsModel(string group, string log, ExceptionSorts sort, int? count = null, Guid? prevLast = null, int? loadAsync = null)
        {
            FixNames(ref group, ref log);

            var errors = GetAllErrors(group, log, sort: sort);

            var startIndex = 0;
            if (prevLast.HasValue)
            {
                startIndex = errors.FindIndex(e => e.GUID == prevLast.Value);
                if (startIndex > 0 && startIndex < errors.Count) startIndex++;
            }
            errors = errors.Skip(startIndex).Take(count ?? 500).ToList();
            var vd = new ExceptionsModel
            {
                Sort = sort,
                SelectedGroup = group,
                SelectedLog = log,
                LoadAsyncSize = loadAsync.GetValueOrDefault(),
                Groups = ApplicationGroups,
                Errors = errors.ToList()
            };
            return vd;
        }

        [Route("exceptions/similar")]
        public async Task<ActionResult> Similar(string group, string log, Guid id, ExceptionSorts? sort = null, bool truncate = true, bool byTime = false)
        {
            // Defaults
            sort = sort ?? ExceptionSorts.TimeDesc;
            FixNames(ref group, ref log);
            var e = await GetError(log, id);
            if (e == null)
                return View("Exceptions.Detail", null);

            var errors = await GetSimilarErrorsAsync(e, byTime, sort: sort.Value);
            var vd = new ExceptionsModel
            {
                Sort = sort.Value,
                Exception = e,
                SelectedGroup = group,
                SelectedLog = log,
                ShowingWindow = byTime,
                Groups = ApplicationGroups,
                ClearLinkForVisibleOnly = true,
                Errors = errors
            };
            return View("Exceptions.Similar", vd);
        }

        [Route("exceptions/search")]
        public async Task<ActionResult> Search(string q, string group, string log, ExceptionSorts? sort = null, bool showDeleted = false)
        {
            // Defaults
            sort = sort ?? ExceptionSorts.TimeDesc;
            FixNames(ref group, ref log);

            // empty searches go back to the main log
            if (q.IsNullOrEmpty())
                return RedirectToAction(nameof(Exceptions), new { group, log });

            var errors = await FindErrorsAsync(q, group, log, includeDeleted: showDeleted, max: 2000, sort: sort.Value);
            if (!errors.Any() && !showDeleted)
            {
                // If we didn't find any current errors, go ahead and search deleted as well
                return RedirectToAction(nameof(Search), new { q, group, log, showDeleted = true });
            }

            var vd = new ExceptionsModel
            {
                Sort = sort.Value,
                Search = q,
                SelectedGroup = group,
                SelectedLog = log,
                ShowDeleted = showDeleted,
                Groups = ApplicationGroups,
                ClearLinkForVisibleOnly = true,
                Errors = errors
            };
            return View("Exceptions.Search", vd);
        }

        [Route("exceptions/detail")]
        public async Task<ActionResult> Detail(string app, Guid id)
        {
            var e = await GetError(app, id);
            return View("Exceptions.Detail", e);
        }

        [Route("exceptions/preview")]
        public async Task<ActionResult> Preview(string app, Guid id)
        {
            var e = await GetError(app, id);
            return View("Exceptions.Preview", e);
        }

        [Route("exceptions/detail/json"), AlsoAllow(Roles.Anonymous)]
        public async Task<JsonResult> DetailJson(string app, Guid id)
        {
            var e = await GetError(app, id);
            return e != null
                       ? Json(new
                       {
                           e.GUID,
                           e.ErrorHash,
                           e.ApplicationName,
                           e.Type,
                           e.Source,
                           e.Message,
                           e.Detail,
                           e.MachineName,
                           e.SQL,
                           e.Host,
                           e.Url,
                           e.HTTPMethod,
                           e.IPAddress,
                           e.DuplicateCount,
                           CreationDate = e.CreationDate.ToEpochTime(),
                       })
                       : JsonNotFound();
        }

        [Route("exceptions/protect"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> Protect(string log, Guid id)
        {
            var success = await ActionAsync(log, s => s.ProtectErrorAsync(id));
            return success ? Counts() : JsonError("Unable to protect, error was not found in the log");
        }

        [Route("exceptions/delete"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> Delete(string log, Guid id, bool redirect = false)
        {
            // we don't care about success...if it's *already* deleted, that's fine
            // if we throw an exception trying to delete, that's another matter
            await ActionAsync(log, s => s.DeleteErrorAsync(id));

            return redirect ? Json(new { url = Url.Action("Exceptions", new { log }) }) : Counts();
        }

        [Route("exceptions/delete-all"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> DeleteAll(string log)
        {
            await ActionAsync(log, s => s.DeleteAllErrorsAsync(new List<string> {log}));

            return Json(new { url = Url.Action("Exceptions") });
        }

        [Route("exceptions/delete-similar"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> DeleteSimilar(string log, Guid id)
        {
            var e = await GetError(log, id);
            await ActionAsync(e.ApplicationName, s => s.DeleteSimilarErrorsAsync(e));

            return Json(new { url = Url.Action("Exceptions", new { log }) });
        }

        [Route("exceptions/delete-list"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> DeleteList(Guid[] ids, bool returnCounts = false)
        {
            if (ids == null || ids.Length == 0) return Json(true);
            await ActionAsync(null, s => s.DeleteErrorsAsync(ids.ToList()));

            return returnCounts ? Counts() : Json(new { url = Url.Action("Exceptions") });
        }

        [Route("exceptions/counts")]
        public ActionResult Counts()
        {
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
                Groups = groups,
                Total = TotalExceptionCount
            });
        }

        [Route("exceptions/jiraactions"), AcceptVerbs(HttpVerbs.Get), OnlyAllow(Roles.ExceptionsAdmin)]
        public ActionResult JiraActions(string appName)
        {
            var issues = JiraSettings.GetActionsForApplication(appName);
            return View("Exceptions.Jira", issues);
        }

        [Route("exceptions/jiraaction"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> JiraAction(string log, Guid id, int actionid, bool redirect = false)
        {
            var e = await GetError(log, id);
            var user = Current.User;
            var action = JiraSettings.Actions.FirstOrDefault(i => i.Id == actionid);
            var jiraClient = new JiraClient(JiraSettings);
            var result = await jiraClient.CreateIssueAsync(action, e, user == null ? "" : user.AccountName);

            if (result.Key.IsNullOrWhiteSpace())
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