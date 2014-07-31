using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Exceptions;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;
using StackExchange.Opserver.Views.Exceptions;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Exceptions)] 
    public class ExceptionsController : StatusController
    {
        protected override ISecurableSection SettingsSection
        {
            get { return Current.Settings.Exceptions; }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            base.OnActionExecuting(filterContext); 
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
        public ActionResult ExceptionsSimilar(string app, Guid id, ExceptionSorts? sort = null, bool truncate = true, bool byTime = false)
        {
            // Defaults
            sort = sort ?? ExceptionSorts.TimeDesc;

            var e = ExceptionStores.GetError(app, id);
            if (e == null)
                return View("Exceptions.Detail", null);

            var errors = ExceptionStores.GetSimilarErrors(e, byTime, sort: sort.Value);
            var vd = new ExceptionsModel
            {
                Sort = sort.Value,
                Exception = e,
                SelectedLog = app,
                ShowingWindow = byTime,
                Applications = ExceptionStores.Applications,
                ClearLinkForVisibleOnly = true,
                Errors = errors
            };
            return View("Exceptions.Similar", vd);
        }

        [Route("exceptions/search")]
        public ActionResult ExceptionsSearch(string q, string log, ExceptionSorts? sort = null, bool showDeleted = false)
        {
            // Defaults
            sort = sort ?? ExceptionSorts.TimeDesc;

            // empty searches go back to the main log
            if (q.IsNullOrEmpty())
                return RedirectToAction("Exceptions", new { log });

            var errors = ExceptionStores.FindErrors(q, log, includeDeleted: showDeleted, max: 2000, sort: sort.Value);
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
        public ActionResult ExceptionDetail(string app, Guid id)
        {
            var e = ExceptionStores.GetError(app, id);
            return View("Exceptions.Detail", e);
        }

        [Route("exceptions/preview")]
        public ActionResult ExceptionPreview(string app, Guid id)
        {
            var e = ExceptionStores.GetError(app, id);
            return View("Exceptions.Preview", e);
        }

        [Route("exceptions/detail/json"), AlsoAllow(Roles.Anonymous)]
        public ActionResult ExceptionDetailJson(string app, Guid id)
        {
            var e = ExceptionStores.GetError(app, id);
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
        public ActionResult ExceptionsProtect(string log, Guid id)
        {
            var success = ExceptionStores.Action(log, s => s.ProtectError(id));
            return success ? ExceptionCounts() : JsonError("Unable to protect, error was not found in the log");
        }

        [Route("exceptions/delete"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public ActionResult ExceptionsDelete(string log, Guid id, bool redirect = false)
        {
            // we don't care about success...if it's *already* deleted, that's fine
            // if we throw an exception trying to delete, that's another matter
            ExceptionStores.Action(log, s => s.DeleteError(id));

            return redirect ? Json(new { url = Url.Action("Exceptions", new { log }) }) : ExceptionCounts();
        }

        [Route("exceptions/delete-all"), HttpPost, AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public ActionResult ExceptionsDeleteAll(string log)
        {
            ExceptionStores.Action(log, s => s.DeleteAllErrors(log));

            return Json(new { url = Url.Action("Exceptions") });
        }

        [Route("exceptions/delete-similar"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public ActionResult ExceptionsDeleteSimilar(string log, Guid id)
        {
            var e = ExceptionStores.GetError(log, id);
            ExceptionStores.Action(e.ApplicationName, s => s.DeleteSimilarErrors(e));

            return Json(new { url = Url.Action("Exceptions", new { log }) });
        }

        [Route("exceptions/delete-list"), AcceptVerbs(HttpVerbs.Post), OnlyAllow(Roles.ExceptionsAdmin)]
        public ActionResult ExceptionsDeleteList(string log, Guid[] ids, bool returnCounts = false)
        {
            if (ids == null || ids.Length == 0) return Json(true);
            ExceptionStores.Action(log, s => s.DeleteErrors(log, ids.ToList()));

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
    }
}