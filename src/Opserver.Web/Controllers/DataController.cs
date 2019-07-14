using System.Text.Json.Serialization;
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

        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions()
        {
            IgnoreNullValues = true,
            WriteIndented = true
        };

        public DataController(IOptions<OpserverSettings> _settings, PollingService poller) : base(_settings) => Poller = poller;

        [Route("json/{type}")]
        public ActionResult JsonNodes(string type, bool includeData = false)
        {
            if (type.IsNullOrEmpty()) return JsonNullParam("Type");

            return Json(DataApi.GetType(Poller, type, includeData), _serializerOptions);
        }

        [Route("json/{type}/{nodeKey}")]
        public ActionResult JsonNode(string type, string nodeKey, bool includeData = false)
        {
            if (type.IsNullOrEmpty()) return JsonNullParam("Type");
            if (nodeKey.IsNullOrEmpty()) return JsonNullParam("NodeKey");

            var node = Poller.GetNode(type, nodeKey);
            if (node == null)
                return JsonCacheError($"No {type} node found with key: {nodeKey}");

            return Json(DataApi.GetNode(node, includeData));
        }

        [Route("json/{type}/{nodeKey}/{cacheKey}", Order = 2)]
        public ActionResult JsonCache(string type, string nodeKey, string cacheKey)
        {
            if (type.IsNullOrEmpty()) return JsonNullParam("Type");
            if (nodeKey.IsNullOrEmpty()) return JsonNullParam("NodeKey");
            if (cacheKey.IsNullOrEmpty()) return JsonNullParam("CacheKey");

            var node = Poller.GetNode(type, nodeKey);
            if (node == null)
                return JsonCacheError($"No {type} node found with key: {nodeKey}");

            var cache = node.GetCache(cacheKey);
            if (cache == null)
                return JsonCacheError($"{nodeKey} node was found, but no cache for property: {cacheKey}");

            return Json(DataApi.GetCache(cache, true));
        }

        private ActionResult JsonNullParam(string param) => JsonCacheError(param + " cannot be null, usage is /data/{type}/{nodeKey}/{cacheKey}");
        private ActionResult JsonCacheError(string message) => JsonError(new { error = message });
    }
}
