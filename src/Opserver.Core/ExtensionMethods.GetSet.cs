using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Opserver.Helpers;

namespace Opserver
{
    public static partial class ExtensionMethods
    {
        private static readonly ConcurrentDictionary<string, object> _getSetNullLocks = new ConcurrentDictionary<string, object>();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _getSetSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();

        internal class GetSetWrapper<T>
        {
            public DateTime StaleAfter { get; set; }
            public T Data { get; set; }
        }

        // return true if this caller won the race to load whatever would go at key
        private static bool GotCompeteLock(LocalCache cache, string key)
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
        private static void ReleaseCompeteLock(LocalCache cache, string key) => cache.Remove(key + "-cload");

        private static SemaphoreSlim GetNullSemaphore(ref SemaphoreSlim semaphore, string key)
        {
            if (semaphore == null)
            {
                semaphore = _getSetSemaphores.AddOrUpdate(key, _ => new SemaphoreSlim(1), (_, old) => old);
            }
            return semaphore;
        }

        private static int totalGetSetSync, totalGetSetAsyncSuccess, totalGetSetAsyncError;
        /// <summary>
        /// Indicates how many sync (first), async-success (second) and async-error (third) GetSet operations have been completed
        /// </summary>
        public static Tuple<int, int, int> GetGetSetStatistics() =>
            Tuple.Create(Interlocked.CompareExchange(ref totalGetSetSync, 0, 0),
                Interlocked.CompareExchange(ref totalGetSetAsyncSuccess, 0, 0),
                Interlocked.CompareExchange(ref totalGetSetAsyncError, 0, 0));

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
        public static T GetSet<T>(this LocalCache cache, string key, Func<T, MicroContext, T> lookup, TimeSpan duration, TimeSpan staleDuration)
            where T : class
        {
            var possiblyStale = cache.Get<GetSetWrapper<T>>(key);
            var localLockName = key;
            var nullLoadLock = _getSetNullLocks.AddOrUpdate(localLockName, _ => new object(), (_, old) => old);
            if (possiblyStale == null)
            {
                // We can't prevent multiple web server's from running this (well, we can but its probably overkill) but we can
                //   at least stop the query from running multiple times on *this* web server
                lock (nullLoadLock)
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
                        Interlocked.Increment(ref totalGetSetSync);
                    }
                }
            }

            if (possiblyStale.StaleAfter > DateTime.UtcNow) return possiblyStale.Data;

            bool gotCompeteLock = false;
            if (Monitor.TryEnter(nullLoadLock, 0))
            {   // it isn't actively being refreshed; we'll check for a mutex on the cache
                try
                {
                    gotCompeteLock = GotCompeteLock(cache, key);
                }
                finally
                {
                    Monitor.Exit(nullLoadLock);
                }
            }

            if (gotCompeteLock)
            {
                var old = possiblyStale.Data;
                var task = new Task(delegate
                {
                    lock (nullLoadLock) // holding this lock allows us to locally short-circuit all the other threads that come asking
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
                        Interlocked.Increment(ref totalGetSetAsyncError);
                        t.Exception.Log();
                    }
                    else
                    {
                        Interlocked.Increment(ref totalGetSetAsyncSuccess);
                    }
                });
                task.Start();
            }

            return possiblyStale.Data;
        }

        // In case there is some context we want here later...
        public class MicroContext : IDisposable
        {
            void IDisposable.Dispose() { }
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
            this LocalCache cache,
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
            LocalCache cache,
            string key,
            Func<T, MicroContext, Task<T>> lookup,
            TimeSpan duration,
            TimeSpan staleDuration)
        {
            GetSetWrapper<T> possiblyStale;
            SemaphoreSlim nullLoadSemaphore = null;

            // We can't prevent multiple web server's from running this (well, we can but its probably overkill) but we can
            //   at least stop the query from running multiple times on *this* web server
            await GetNullSemaphore(ref nullLoadSemaphore, key).WaitAsync();
            try
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

                    Interlocked.Increment(ref totalGetSetSync);
                }
            }
            finally
            {
                nullLoadSemaphore.Release();
            }

            if (possiblyStale?.StaleAfter > DateTime.UtcNow)
            {
                return possiblyStale.Data;
            }

            var gotCompeteLock = false;

            if (await GetNullSemaphore(ref nullLoadSemaphore, key).WaitAsync(0))
            {
                try
                {
                    gotCompeteLock = GotCompeteLock(cache, key);
                }
                finally
                {
                    nullLoadSemaphore.Release();
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
                        await nullLoadSemaphore.WaitAsync();
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
                            Interlocked.Increment(ref totalGetSetAsyncSuccess);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref totalGetSetAsyncError);
                            ex.Log();
                        }
                        finally
                        {
                            ReleaseCompeteLock(cache, key);
                            nullLoadSemaphore.Release();
                        }
                    });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }

            return possiblyStale.Data;
        }
    }
}
