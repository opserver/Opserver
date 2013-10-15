using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;

namespace StackExchange.Status.Helpers
{
    public class LocalCache
    {
        protected object _lock = new object();
        
        public bool Exists(string key)
        {
            return HttpRuntime.Cache[key] != null;
        }

        /// <summary>
        /// Gets an item of type T from local cache
        /// </summary>
        public T Get<T>(string key)
        {
            var o = HttpRuntime.Cache[key];
            if (o == null) return default(T);
            if (o is T)
                return (T)o;
            return default(T);
        }

        /// <summary>
        /// Places an item of type T into local cache for the specified duration
        /// </summary>
        public void Set<T>(string key, T value, int? durationSecs, bool sliding = false)
        {
            SetWithPriority<T>(key, value, durationSecs, sliding, CacheItemPriority.Default);
        }

        public void SetWithPriority<T>(string key, T value, int? durationSecs, bool isSliding, CacheItemPriority priority)
        {
            RawSet(key, value, durationSecs, isSliding, priority);
        }

        private void RawSet(string cacheKey, object value, int? durationSecs, bool isSliding, CacheItemPriority priority)
        {
            var absolute = !isSliding && durationSecs.HasValue ? DateTime.UtcNow.AddSeconds(durationSecs.Value) : System.Web.Caching.Cache.NoAbsoluteExpiration;
            var sliding = isSliding && durationSecs.HasValue ? TimeSpan.FromSeconds(durationSecs.Value) : System.Web.Caching.Cache.NoSlidingExpiration;

            HttpRuntime.Cache.Insert(cacheKey, value, null, absolute, sliding, priority, null);
        }

        /// <summary>
        /// Removes an item from local cache
        /// </summary>
        public void Remove(string key)
        {
            lock (_lock)
            {
                HttpRuntime.Cache.Remove(key);
            }
        }

        /// <summary>
        /// Removes a pattern from local cache, e.g. "MyBase-*" would remove anything beginning with "MyBase-"
        /// </summary>
        public void RemoveAll(string pattern)
        {
            var patternRegEx = new Regex(pattern.Replace(".", "[.]").Replace("*", ".*").Replace("?", "."), RegexOptions.Compiled);
            lock(_lock)
            {
                var keysToRemove = new List<string>();
                var e = HttpRuntime.Cache.GetEnumerator();
                while (e.MoveNext())
                {
                    if (patternRegEx.IsMatch(e.Key.ToString())) 
                        keysToRemove.Add(e.Key.ToString());
                }
                keysToRemove.ForEach(k => HttpRuntime.Cache.Remove(k));
            }
        }


        public bool SetNXSync<T>(string key, T val)
        {
            lock (_lock)
            {
                if (Get<T>(key).Equals(default(T)))
                {
                    Set<T>(key, val, null, false);
                    return true;
                }
                return false;
            }
        }
    }
}