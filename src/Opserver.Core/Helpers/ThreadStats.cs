using System.Threading;

namespace Opserver.Helpers
{
    public class ThreadStats
    {
        private readonly int _minWorkerThreads;
        public int MinWorkerThreads => _minWorkerThreads;

        private readonly int _minIOThreads;
        public int MinIOThreads => _minIOThreads;

        private readonly int _availableWorkerThreads;
        public int AvailableWorkerThreads => _availableWorkerThreads;

        private readonly int _availableIOThreads;
        public int AvailableIOThreads => _availableIOThreads;

        private readonly int _maxIOThreads;
        public int MaxIOThreads => _maxIOThreads;

        private readonly int _maxWorkerThreads;
        public int MaxWorkerThreads => _maxWorkerThreads;

        public int BusyIOThreads => _maxIOThreads - _availableIOThreads;
        public int BusyWorkerThreads => _maxWorkerThreads - _availableWorkerThreads;

        public ThreadStats()
        {
            ThreadPool.GetMinThreads(out _minWorkerThreads, out _minIOThreads);
            ThreadPool.GetAvailableThreads(out _availableWorkerThreads, out _availableIOThreads);
            ThreadPool.GetMaxThreads(out _maxWorkerThreads, out _maxIOThreads);
        }
    }
}
