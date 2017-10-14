using StackExchange.Opserver.Data;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;
using System;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Helpers
{
    public class MiniProfilerCacheStorage : MemoryCacheStorage, IAsyncStorage
    {
        public MiniProfilerCacheStorage(TimeSpan cacheDuration) : base(cacheDuration) {}

        MiniProfiler IAsyncStorage.Load(Guid id) => Load(id) ?? PollingEngine.GetCache(id)?.Profiler;
        Task<MiniProfiler> IAsyncStorage.LoadAsync(Guid id) => LoadAsync(id) ?? Task.FromResult(PollingEngine.GetCache(id)?.Profiler);
    }
}
