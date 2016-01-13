using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;

namespace StackExchange.Opserver.Monitoring
{
    public class MiniProfilerNullStorage : IStorage
    {
        public static MiniProfilerNullStorage Instance { get; } = new MiniProfilerNullStorage();

        public IEnumerable<Guid> List(int maxResults, DateTime? start = null, DateTime? finish = null, ListResultsOrder orderBy = ListResultsOrder.Descending) => Enumerable.Empty<Guid>();
        public void Save(MiniProfiler profiler) { }
        public MiniProfiler Load(Guid id) => null;
        public void SetUnviewed(string user, Guid id) { }
        public void SetViewed(string user, Guid id) { }
        public List<Guid> GetUnviewedIds(string user) => new List<Guid>();
    }
}
