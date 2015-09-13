using System;
using System.Threading.Tasks;
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
        public Task<ActionResult> KillClient(string node, string address)
        {
            return PerformInstanceAction(node, i => i.KillClientAsync(address));
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
                oldMaster?.Poll(true);
                return Json(new { message });
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [Route("redis/instance/actions/slave-to"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public Task<ActionResult> SlaveServer(string node, string newMaster)
        {
            return PerformInstanceAction(node, i => i.SlaveToAsync(newMaster), poll: true);
        }

        [Route("redis/instance/actions/set-tiebreaker"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public Task<ActionResult> SetTiebreaker(string node)
        {
            return PerformInstanceAction(node, i => i.SetSERedisTiebreakerAsync());
        }

        [Route("redis/instance/actions/clear-tiebreaker"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public Task<ActionResult> ClearTiebreaker(string node)
        {
            return PerformInstanceAction(node, i => i.ClearSERedisTiebreakerAsync());
        }

        private async Task<ActionResult> PerformInstanceAction(string node, Func<RedisInstance, Task<bool>> action, bool poll = false)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return JsonNotFound();

            try
            {
                var success = await action(i);
                if (poll) i.Poll(true);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }
    }
}
