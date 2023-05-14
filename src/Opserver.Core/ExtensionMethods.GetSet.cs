using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Microsoft.Extensions.Caching.Memory;

namespace Opserver
{
    public static partial class ExtensionMethods
    {
        private static readonly object _syncLock = new object();
        private static readonly AsyncKeyedLocker<string> _asyncKeyedLocker = new AsyncKeyedLocker<string>(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });

        internal class GetSetWrapper<T>
        {
            public DateTime StaleAfter { get; set; }
            public T Data { get; set; }
        }

        /// <summary>
        /// A context passed to GetSet methods, in case there is some context we want here later.
        /// </summary>
        public class MicroContext : IDisposable
        {
            void IDisposable.Dispose() { }
        }

        /// <summary>
        /// Sets a value in cache *only if it's not already there*.
        /// Note: this requires all competition to flow through the same method (as it uses a lock specific to this path).
        /// </summary>
        /// <typeparam name="T">The type to insert into cache.</typeparam>
        /// <param name="cache">The cache to put the value in.</param>
        /// <param name="key">The key in cache to use.</param>
        /// <param name="val">The value to insert.</param>
        /// <returns>True if the value was inserted (not already present), false if it already existed.</returns>
        public static bool SetNXSync<T>(this IMemoryCache cache, string key, T val)
        {
            lock (_syncLock)
            {
                if (cache.Get<T>(key).Equals(default(T)))
                {
                    cache.Set(key, val);
                    return true;
                }
                return false;
            }
        }

        // return true if this caller won the race to load whatever would go at key
        private static bool GotCompeteLock(IMemoryCache cache, string key)
        {
            while (true)
            {
                var competeKey = key + "-cload";
                if (cache.SetNXSync(competeKey, DateTime.UtcNow))
                {
                    // Got it!
                    return true;
                }

                var x = cache.Get<DateTime>(competeKey);
                // Did somebody abandoned the lock?
                if (DateTime.UtcNow - x > TimeSpan.FromMinutes(5))
                {
                    // Yep, clear it and try again
                    cache.Remove(competeKey);
                    continue;
                }
                // Lost the lock competition
                return false;
            }
        }

        // called by a winner of CompeteToLoad, to make it so the next person to call CompeteToLoad will get true
        private static void ReleaseCompeteLock(IMemoryCache cache, string key) => cache.Remove(key + "-cload");

        private static int _totalGetSetSync, _totalGetSetAsyncSuccess, _totalGetSetAsyncError;
        /// <summary>
        /// Indicates how many sync (first), async-success (second) and async-error (third) GetSet operations have been completed
        /// </summary>
        public static Tuple<int, int, int> GetGetSetStatistics() =>
            Tuple.Create(Interlocked.CompareExchange(ref _totalGetSetSync, 0, 0),
                Interlocked.CompareExchange(ref _totalGetSetAsyncSuccess, 0, 0),
                Interlocked.CompareExchange(ref _totalGetSetAsyncError, 0, 0));

        /// <summary>
        /// Gets the current data via <paramref name="lookup"/>. 
        /// When data is missing or stale (older than <paramref name="duration"/>), a background refresh is 
        /// initiated on stale fetches still within <paramref name="staleDuration"/> after <paramref name="duration"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to get.</typeparam>
        /// <param name="cache">The cache to use.</param>
        /// <param name="key">The cache key to use.</param>
        /// <param name="lookup">Refreshes the data if necessary, passing the old data if we have it.</param>
        /// <param name="duration">The time to cache the data, before it's considered stale.</param>
        /// <param name="staleDuration">The time available to serve stale data, while a background fetch is performed.</param>
        public static T GetSet<T>(this IMemoryCache cache, string key, Func<T, MicroContext, T> lookup, TimeSpan duration, TimeSpan staleDuration)
            where T : class
        {
            var possiblyStale = cache.Get<GetSetWrapper<T>>(key);
            if (possiblyStale == null)
            {
                // We can't prevent multiple web server's from running this (well, we can but its probably overkill) but we can
                //   at least stop the query from running multiple times on *this* web server
                using (_asyncKeyedLocker.Lock(key))
                {
                    possiblyStale = cache.Get<GetSetWrapper<T>>(key);

                    if (possiblyStale == null)
                    {
                        T data;
                        using (var ctx = new MicroContext())
                        {
                            data = lookup(null, ctx);
                        }
                        possiblyStale = new GetSetWrapper<T>
                        {
                            Data = data,
                            StaleAfter = DateTime.UtcNow + duration
                        };

                        cache.Set(key, possiblyStale, duration + staleDuration);
                        Interlocked.Increment(ref _totalGetSetSync);
                    }
                }
            }

            if (possiblyStale.StaleAfter > DateTime.UtcNow) return possiblyStale.Data;

            bool gotCompeteLock = false;
            if (!_asyncKeyedLocker.IsInUse(key))
            {   // it isn't actively being refreshed; we'll check for a mutex on the cache
                try
                {
                    gotCompeteLock = GotCompeteLock(cache, key);
                }
                finally { }
            }

            if (gotCompeteLock)
            {
                var old = possiblyStale.Data;
                var task = new Task(delegate
                {
                    using (_asyncKeyedLocker.Lock(key)) // holding this lock allows us to locally short-circuit all the other threads that come asking
                    {
                        try
                        {
                            var updated = new GetSetWrapper<T>();
                            using (var ctx = new MicroContext())
                            {
                                updated.Data = lookup(old, ctx);
                                updated.StaleAfter = DateTime.UtcNow + duration;
                            }
                            cache.Remove(key);
                            cache.Set(key, updated, duration + staleDuration);
                        }
                        finally
                        {
                            ReleaseCompeteLock(cache, key);
                        }
                    }
                });
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Interlocked.Increment(ref _totalGetSetAsyncError);
                        t.Exception.Log();
                    }
                    else
                    {
                        Interlocked.Increment(ref _totalGetSetAsyncSuccess);
                    }
                });
                task.Start();
            }

            return possiblyStale.Data;
        }

        /// <summary>
        /// Asynchronously gets the current data via <paramref name="lookup"/>. 
        /// When data is missing or stale (older than <paramref name="duration"/>), a background refresh is 
        /// initiated on stale fetches still within <paramref name="staleDuration"/> after <paramref name="duration"/>.
        /// </summary>
        /// <typeparam name="T">The type of value to get.</typeparam>
        /// <param name="cache">The cache to use.</param>
        /// <param name="key">The cache key to use.</param>
        /// <param name="lookup">Refreshes the data if necessary, passing the old data if we have it.</param>
        /// <param name="duration">The time to cache the data, before it's considered stale.</param>
        /// <param name="staleDuration">The time available to serve stale data, while a background fetch is performed.</param>
        public static Task<T> GetSetAsync<T>(
            this IMemoryCache cache,
            string key,
            Func<T, MicroContext, Task<T>> lookup,
            TimeSpan duration,
            TimeSpan staleDuration)
        {
            var possiblyStale = cache.Get<GetSetWrapper<T>>(key);
            if (possiblyStale?.StaleAfter > DateTime.UtcNow)
            {
                return Task.FromResult(possiblyStale.Data);
            }

            return GetSetAsyncFallback(cache, key, lookup, duration, staleDuration);
        }

        private static async Task<T> GetSetAsyncFallback<T>(
            IMemoryCache cache,
            string key,
            Func<T, MicroContext, Task<T>> lookup,
            TimeSpan duration,
            TimeSpan staleDuration)
        {
            GetSetWrapper<T> possiblyStale;

            // We can't prevent multiple web server's from running this (well, we can but its probably overkill) but we can
            //   at least stop the query from running multiple times on *this* web server
            using (await _asyncKeyedLocker.LockAsync(key).ConfigureAwait(false))
            {
                possiblyStale = cache.Get<GetSetWrapper<T>>(key);

                if (possiblyStale == null)
                {
                    T data;
                    using (var ctx = new MicroContext())
                    {
                        data = await lookup(default, ctx);
                    }

                    possiblyStale = new GetSetWrapper<T>
                    {
                        Data = data,
                        StaleAfter = DateTime.UtcNow + duration
                    };

                    cache.Set(key, possiblyStale, duration + staleDuration);

                    Interlocked.Increment(ref _totalGetSetSync);
                }
            }

            if (possiblyStale?.StaleAfter > DateTime.UtcNow)
            {
                return possiblyStale.Data;
            }

            var gotCompeteLock = false;

            using (var releaser = await _asyncKeyedLocker.LockAsync(key, 0).ConfigureAwait(false))
            {
                if (releaser.EnteredSemaphore)
                {
                    try
                    {
                        gotCompeteLock = GotCompeteLock(cache, key);
                    }
                    finally { }
                }
            }

            if (gotCompeteLock)
            {
                var old = possiblyStale.Data;
                // The using is okay because we just need suppression when creating the task
                using (ExecutionContext.SuppressFlow())
                {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Task.Run(async () =>
                    {
                        using (await _asyncKeyedLocker.LockAsync(key).ConfigureAwait(false))
                        {
                            try
                            {
                                GetSetWrapper<T> updated;
                                using (var ctx = new MicroContext())
                                {
                                    updated = new GetSetWrapper<T>
                                    {
                                        Data = await lookup(old, ctx),
                                        StaleAfter = DateTime.UtcNow + duration
                                    };
                                }

                                cache.Remove(key);
                                cache.Set(key, updated, duration + staleDuration);
                                Interlocked.Increment(ref _totalGetSetAsyncSuccess);
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref _totalGetSetAsyncError);
                                ex.Log();
                            }
                            finally
                            {
                                ReleaseCompeteLock(cache, key);
                            }
                        }
                    });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }

            return possiblyStale.Data;
        }
    }
}
