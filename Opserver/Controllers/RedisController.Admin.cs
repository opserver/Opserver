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

        [Route("redis/instance/actions/{node}"), OnlyAllow(Roles.RedisAdmin)]
        public ActionResult InstanceActions(string node)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return JsonNotFound();

            return View("Instance.Actions", i);
        }

        [Route("redis/instance/actions/{node}/make-master"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public ActionResult PromoteToMaster(string node)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return JsonNotFound();

            var oldMaster = i.Master;
            try
            {
                var message = i.PromoteToMaster();
                // We want these to be synchronous
                i.Poll(true);
                oldMaster?.Poll(true);
                return Json(new { message });
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [Route("redis/instance/actions/{node}/key-purge"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public async Task<ActionResult> KeyPurge(string node, int db, string key)
        {
            var i = RedisInstance.GetInstance(node);
            if (i == null) return JsonNotFound();
            
            try
            {
                var removed = await i.KeyPurge(db, key);
                return Json(new {removed});
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [Route("redis/instance/actions/{node}/slave-to"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public Task<ActionResult> SlaveServer(string node, string newMaster)
        {
            return PerformInstanceAction(node, i => i.SlaveToAsync(newMaster), poll: true);
        }

        [Route("redis/instance/actions/{node}/set-tiebreaker"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
        public Task<ActionResult> SetTiebreaker(string node)
        {
            return PerformInstanceAction(node, i => i.SetSERedisTiebreakerAsync());
        }

        [Route("redis/instance/actions/{node}/clear-tiebreaker"), HttpPost, OnlyAllow(Roles.RedisAdmin)]
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
                if (poll) i.PollAsync(true);
                return Json(new { success });
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }
    }
}
