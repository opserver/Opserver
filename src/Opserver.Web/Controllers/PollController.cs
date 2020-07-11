using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data;
using Opserver.Helpers;
using Opserver.Models;

namespace Opserver.Controllers
{
    [OnlyAllow(Roles.Authenticated)]
    public class PollController : StatusController
    {
        private PollingService Poller { get; }

        public PollController(IOptions<OpserverSettings> _settings, PollingService poller) : base(_settings) => Poller = poller;

        [Route("poll")]
        public async Task<ActionResult> PollNodes(string type, string[] key, Guid? guid = null)
        {
            if (type.IsNullOrEmpty())
                return JsonError("type is missing");
            if (!(key?.Any() ?? false))
                return JsonError("key is missing");
            try
            {
                var polls = key.Select(k => Poller.PollAsync(type, k, guid));
                var results = await Task.WhenAll(polls);
                return Json(results.Aggregate(true, (current, r) => current && r));
            }
            catch (Exception e)
            {
                return JsonError("Error polling node: " + e.Message);
            }
        }
    }
}
