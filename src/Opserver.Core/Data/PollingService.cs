using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Opserver.Data
{
    public class PollingService : IHostedService
    {
        private readonly IMemoryCache _memCache;
        private readonly ILogger _logger;
        private CancellationToken _cancellationToken;

        public PollingService(IMemoryCache cache, ILogger<PollingService> logger)
        {
            _memCache = cache;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _logger.LogInformation("Polling service is starting.");
            PollingEngine.StartPolling(_memCache, _cancellationToken);
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
