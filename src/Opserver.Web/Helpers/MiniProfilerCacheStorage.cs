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
        private readonly PollingService _poller;
        public MiniProfilerCacheStorage(IMemoryCache cache, PollingService poller, TimeSpan cacheDuration) : base(cache, cacheDuration)
        {
            _poller = poller;
        }

        MiniProfiler IAsyncStorage.Load(Guid id) => Load(id) ?? _poller.GetCache(id)?.Profiler;
        Task<MiniProfiler> IAsyncStorage.LoadAsync(Guid id) => LoadAsync(id) ?? Task.FromResult(_poller.GetCache(id)?.Profiler);
    }

    internal class MiniProfilerCacheStorageDefaults : IConfigureOptions<MiniProfilerOptions>
    {
        private readonly IMemoryCache _cache;
        private readonly PollingService _poller;
        public MiniProfilerCacheStorageDefaults(IMemoryCache cache, PollingService poller)
        {
            _cache = cache;
            _poller = poller;
        }

        public void Configure(MiniProfilerOptions options)
        {
            if (options.Storage == null)
            {
                options.Storage = new MiniProfilerCacheStorage(_cache, _poller, TimeSpan.FromMinutes(10));
            }
        }
    }
}
