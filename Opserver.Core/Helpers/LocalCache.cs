using System;
using System.Runtime.Caching;

namespace StackExchange.Opserver.Helpers
{
    public class LocalCache
    {
        private static MemoryCache _cache;
        static LocalCache()
        {
            _cache = new MemoryCache("LocalCache");
        }

        protected object _lock = new object();
        
        public bool Exists(string key)
        {
            return _cache[key] != null;
        }

        /// <summary>
        /// Gets an item of type T from local cache
        /// </summary>
        public T Get<T>(string key)
        {
            var o = _cache[key];
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
            var policy = new CacheItemPolicy { Priority = priority };
            if (!isSliding && durationSecs.HasValue)
                policy.AbsoluteExpiration = DateTime.UtcNow.AddSeconds(durationSecs.Value);
            if (isSliding && durationSecs.HasValue)
                policy.SlidingExpiration = TimeSpan.FromSeconds(durationSecs.Value);
            
            _cache.Add(cacheKey, value, policy);
        }

        /// <summary>
        /// Removes an item from local cache
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);
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