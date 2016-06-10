using System;
using StackExchange.Opserver.Data;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;

namespace StackExchange.Opserver.Helpers
{
    public class MiniProfilerCacheStorage : HttpRuntimeCacheStorage, IStorage
    {
        public MiniProfilerCacheStorage(TimeSpan cacheDuration) : base(cacheDuration){}

        MiniProfiler IStorage.Load(Guid id)
        {
            var profiler = base.Load(id);
            if (profiler == null)
            {
                return PollingEngine.GetCache(id)?.Profiler;
            }
            return profiler;
        }
    }
}
