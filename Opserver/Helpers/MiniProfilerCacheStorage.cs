using System;
using StackExchange.Opserver.Data;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;

namespace StackExchange.Opserver.Helpers
{
    public class MiniProfilerCacheStorage : HttpRuntimeCacheStorage, IStorage
    {
        public MiniProfilerCacheStorage(TimeSpan cacheDuration) : base(cacheDuration) {}
        MiniProfiler IStorage.Load(Guid id) => base.Load(id) ?? PollingEngine.GetCache(id)?.Profiler;
    }
}
