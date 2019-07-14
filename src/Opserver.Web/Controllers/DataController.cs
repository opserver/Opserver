using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opserver.Data;
using Opserver.Helpers;
using Opserver.Models;

namespace Opserver.Controllers
{
    [OnlyAllow(Roles.GlobalAdmin | Roles.InternalRequest)]
    public class DataController : StatusController
    {
        private PollingService Poller { get; }

        public DataController(IOptions<OpserverSettings> _settings, PollingService poller) : base(_settings) => Poller = poller;

        [Route("json/{type}")]
        public ActionResult JsonNodes(string type, bool cacheData = false)
        {
            if (type.IsNullOrEmpty()) return JsonNullParam("Type");
            try
            {
                return JsonRaw(JsonApi.Get(Poller, type, includeCaches: cacheData));
            }
            catch (JsonApiException e)
            {
                return JsonCacheError(e);
            }
        }

        [Route("json/{type}/{key}")]
        public ActionResult JsonNode(string type, string key, bool cacheData = false)
        {
            return JsonCacheInner(type, key, cacheData);
        }

        [Route("json/{type}/{key}/_all", Order = 1)]
        public ActionResult JsonCacheAll(string type, string key)
        {
            return JsonCacheInner(type, key, true);
        }

        private ActionResult JsonCacheInner(string type, string key, bool includeCache)
        {
            if (type.IsNullOrEmpty()) return JsonNullParam("Type");
            if (key.IsNullOrEmpty()) return JsonNullParam("Key");
            try
            {
                return JsonRaw(JsonApi.Get(Poller, type, key, includeCaches: includeCache));
            }
            catch (JsonApiException e)
            {
                return JsonCacheError(e);
            }
        }

        [Route("json/{type}/{key}/{property}", Order = 2)]
        public ActionResult JsonCache(string type, string key, string property)
        {
            if (type.IsNullOrEmpty()) return JsonNullParam("Type");
            if (key.IsNullOrEmpty()) return JsonNullParam("Key");
            if (property.IsNullOrEmpty()) return JsonNullParam("Property");

            try
            {
                return JsonRaw(JsonApi.Get(Poller, type, key, property, includeCaches: true));
            }
            catch (JsonApiException e)
            {
                return JsonCacheError(e);
            }
        }

        private ActionResult JsonNullParam(string param) =>
            JsonError(new { error = param + " cannot be null, usage is /data/{type}/{key}/{property}" });

        private ActionResult JsonCacheError(JsonApiException e) =>
            JsonError(new { error = e.Message });
    }
}
