using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Opserver.Data;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;
using System;
using System.Threading.Tasks;

namespace Opserver.Helpers
{
    public class MiniProfilerCacheStorage : MemoryCacheStorage, IAsyncStorage
    {
        public MiniProfilerCacheStorage(IMemoryCache cache, TimeSpan cacheDuration) : base(cache, cacheDuration) {}

        MiniProfiler IAsyncStorage.Load(Guid id) => Load(id) ?? PollingEngine.GetCache(id)?.Profiler;
        Task<MiniProfiler> IAsyncStorage.LoadAsync(Guid id) => LoadAsync(id) ?? Task.FromResult(PollingEngine.GetCache(id)?.Profiler);
    }

    internal class MiniProfilerCacheStorageDefaults : IConfigureOptions<MiniProfilerOptions>
    {
        private readonly IMemoryCache _cache;
        public MiniProfilerCacheStorageDefaults(IMemoryCache cache) => _cache = cache;

        public void Configure(MiniProfilerOptions options)
        {
            if (options.Storage == null)
            {
                options.Storage = new MiniProfilerCacheStorage(_cache, TimeSpan.FromMinutes(10));
            }
        }
    }
}
