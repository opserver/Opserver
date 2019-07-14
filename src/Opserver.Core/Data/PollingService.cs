using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Opserver.Data
{
    public class PollingService : IHostedService
    {
        private readonly ILogger _logger;
        private CancellationToken _cancellationToken;

        public IMemoryCache MemCache { get; }

        public PollingService(IMemoryCache cache, ILogger<PollingService> logger)
        {
            MemCache = cache;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _logger.LogInformation("Polling service is starting.");
            PollingEngine.StartPolling(MemCache, _cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Polling service is stopping.");
            PollingEngine.StopPolling();
            return Task.CompletedTask;
        }
    }
}
