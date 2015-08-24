using System;
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

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Exceptions)] 
    public class ExceptionsController : StatusController
    {
        protected override ISecurableSection SettingsSection => Current.Settings.Exceptions;

        protected override string TopTab => TopTabs.BuiltIn.Exceptions;

        private JiraSettings JiraSettings => Current.Settings.Jira;

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            base.OnActionExecuting(filterContext); 
        }

        private string GetLogName(string log)
        {
            var apps = ExceptionStores.Applications.ToList();
            var app = apps.FirstOrDefault(a => a.Name == log) ?? apps.FirstOrDefault(a => a.ShortName == log);
            return app != null ? app.Name : log;
        }

        [Route("exceptions")]
        public ActionResult Exceptions(string log, ExceptionSorts? sort = null, int? count = null)
        {
            // Defaults
            count = count ?? 250;
            sort = sort ?? ExceptionSorts.TimeDesc;

            var vd = GetExceptionsModel(log, sort.Value, count.Value, loadAsync: 500);
            return View(vd);
        }

        [Route("exceptions/load-more")]
        public ActionResult ExceptionsLoadMore(string log, ExceptionSorts sort, int? count = null, Guid? prevLast = null)
        {
            var vd = GetExceptionsModel(log, sort, count, prevLast);
            return View("Exceptions.Table.Rows", vd);
        }

        public ExceptionsModel GetExceptionsModel(string log, ExceptionSorts sort, int? count = null, Guid? prevLast = null, int? loadAsync = null)
        {
            log = GetLogName(log);

            var errors = ExceptionStores.GetAllErrors(log, sort: sort);

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
                SelectedLog = log,
                LoadAsyncSize = loadAsync.GetValueOrDefault(),
                Applications = ExceptionStores.Applications,
                Errors = errors.ToList()
            };
            return vd;
        }

        [Route("exceptions/similar")]
        public async Task<ActionResult> ExceptionsSimilar(string log, Guid id, ExceptionSorts? sort = null, bool truncate = true, bool byTime = false)
        {
            // Defaults
            sort = sort ?? ExceptionSorts.TimeDesc;
            log = GetLogName(log);
            var e = await ExceptionStores.GetError(log, id);
            if (e == null)
                return View("Exceptions.Detail", null);

            var errors = await ExceptionStores.GetSimilarErrorsAsync(e, byTime, sort: sort.Value);
            var vd = new ExceptionsModel
            {
                Sort = sort.Value,
                Exception = e,
                SelectedLog = log,
                ShowingWindow = byTime,
                Applications = ExceptionStores.Applications,
                ClearLinkForVisibleOnly = true,
                Errors = errors
            };
            return View("Exceptions.Similar", vd);
        }

        [Route("exceptions/search")]
        public async Task<ActionResult> ExceptionsSearch(string q, string log, ExceptionSorts? sort = null, bool showDeleted = false)
        {
            // Defaults
            sort = sort ?? ExceptionSorts.TimeDesc;
            log = GetLogName(log);

            // empty searches go back to the main log
            if (q.IsNullOrEmpty())
                return RedirectToAction("Exceptions", new { log });

            var errors = await ExceptionStores.FindErrorsAsync(q, log, includeDeleted: showDeleted, max: 2000, sort: sort.Value);
            if (!errors.Any() && !showDeleted)
            {
                // If we didn't find any current errors, go ahead and search deleted as well
                return RedirectToAction("ExceptionsSearch", new { q, log, showDeleted = true });
            }

            var vd = new ExceptionsModel
            {
                Sort = sort.Value,
                Search = q,
                SelectedLog = log,
                ShowDeleted = showDeleted,
                Applications = ExceptionStores.Applications,
                ClearLinkForVisibleOnly = true,
                Errors = errors
            };
            return View("Exceptions.Search", vd);
        }

        [Route("exceptions/detail")]
        public async Task<ActionResult> ExceptionDetail(string app, Guid id)
        {
            var e = await ExceptionStores.GetError(app, id);
            return View("Exceptions.Detail", e);
        }

        [Route("exceptions/preview")]
        public async Task<ActionResult> ExceptionPreview(string app, Guid id)
        {
            var e = await ExceptionStores.GetError(app, id);
            return View("Exceptions.Preview", e);
        }

        [Route("exceptions/detail/json"), AlsoAllow(Roles.Anonymous)]
        public async Task<JsonResult> ExceptionDetailJson(string app, Guid id)
        {
            var e = await ExceptionStores.GetError(app, id);
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
        public async Task<ActionResult> ExceptionsProtect(string log, Guid id)
        {
            var success = await ExceptionStores.Action(log, s => s.ProtectError(id));
            return success ? ExceptionCounts() : JsonError("Unable to protect, error was not found in the log");
        }

        [Route("exceptions/delete"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> ExceptionsDelete(string log, Guid id, bool redirect = false)
        {
            // we don't care about success...if it's *already* deleted, that's fine
            // if we throw an exception trying to delete, that's another matter
            await ExceptionStores.Action(log, s => s.DeleteError(id));

            return redirect ? Json(new { url = Url.Action("Exceptions", new { log }) }) : ExceptionCounts();
        }

        [Route("exceptions/delete-all"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> ExceptionsDeleteAll(string log)
        {
            await ExceptionStores.Action(log, s => s.DeleteAllErrors(log));

            return Json(new { url = Url.Action("Exceptions") });
        }

        [Route("exceptions/delete-similar"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> ExceptionsDeleteSimilar(string log, Guid id)
        {
            var e = await ExceptionStores.GetError(log, id);
            await ExceptionStores.Action(e.ApplicationName, s => s.DeleteSimilarErrors(e));

            return Json(new { url = Url.Action("Exceptions", new { log }) });
        }

        [Route("exceptions/delete-list"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public async Task<ActionResult> ExceptionsDeleteList(string log, Guid[] ids, bool returnCounts = false)
        {
            if (ids == null || ids.Length == 0) return Json(true);
            await ExceptionStores.Action(log, s => s.DeleteErrors(log, ids.ToList()));

            return returnCounts ? ExceptionCounts() : Json(new { url = Url.Action("Exceptions", new { log }) });
        }

        [Route("exceptions/counts")]
        public ActionResult ExceptionCounts()
        {
            return Json(
                ExceptionStores.Applications.GroupBy(a => a.Name).Select(
                    g =>
                    new
                        {
                            Name = g.Key,
                            ExceptionCount = g.Sum(a => a.ExceptionCount),
                            MostRecent = g.Max(a => a.MostRecent)
                        })
                );
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
            var e = await ExceptionStores.GetError(log, id);
            var user = Current.User;
            var action = JiraSettings.Actions.FirstOrDefault(i => i.Id == actionid);
            var jiraClient = new JiraClient(JiraSettings);
            var result = await jiraClient.CreateIssue(action, e, user == null ? String.Empty : user.AccountName);

            if (result.Key.IsNullOrWhiteSpace())
            {
                return Json(new
                {
                    success = false,
                    message = "Can not create issue"
                });
            }
            else
            {
                return Json(new
                {
                    success = true,
                    issueKey = result.Key,
                    browseUrl = result.BrowseUrl
                });
            }

        }
    }
}