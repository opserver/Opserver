using System;
using System.Web.Mvc;
using StackExchange.Opserver.Data.Redis;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.Redis)]
    public partial class RedisController
    {
        [Route("redis/instance/kill-client"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public ActionResult KillClient(string node, string address)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return JsonNotFound();

            try
            {
                bool success = i.KillClient(address);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [Route("redis/instance/actions/role"), HttpGet, OnlyAllow(Roles.RedisAdmin)]
        public ActionResult RoleActions(string node)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return JsonNotFound();

            return View("Dashboard.RoleActions", i);
        }

        [Route("redis/instance/actions/make-master"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public ActionResult PromoteToMaster(string node)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return JsonNotFound();

            var oldMaster = i.Master;

            try
            {
                var message = i.PromoteToMaster();
                i.Poll(true);
                if (oldMaster != null) oldMaster.Poll(true);
                return Json(new { message });
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [Route("redis/instance/actions/slave-to"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public ActionResult SlaveServer(string node, string newMaster)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return JsonNotFound();

            try
            {
                var success = i.SlaveTo(newMaster);
                i.Poll(true);
                return Json(new {success});
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }
    }
}
