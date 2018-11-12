using System.Web.Mvc;
using StackExchange.Opserver.Data;
using StackExchange.Opserver.Helpers;
using StackExchange.Opserver.Models;

namespace StackExchange.Opserver.Controllers
{
    [OnlyAllow(Roles.GlobalAdmin | Roles.InternalRequest)]
    public class DataController : StatusController
    {
        [Route("json/{type}")]
        public ActionResult JsonNodes(string type, bool cacheData = false)
        {
            if (type.IsNullOrEmpty()) return JsonNullParam("Type");
            try
            {
                return JsonRaw(DataProvider.GetNodeJSON(type, includeCaches: cacheData));
            }
            catch (DataPullException e)
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
                return JsonRaw(DataProvider.GetNodeJSON(type, key, includeCaches: includeCache));
            }
            catch (DataPullException e)
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
                return JsonRaw(DataProvider.GetNodeJSON(type, key, property, includeCaches: true));
            }
            catch (DataPullException e)
            {
                return JsonCacheError(e);
            }
        }

        private ActionResult JsonNullParam(string param)
        {
            return JsonError(new { error = param + " cannot be null, usage is /data/{type}/{key}/{property}" });
        }

        private ActionResult JsonCacheError(DataPullException e)
        {
            return JsonError(new { error = e.Message });
        }
    }
}