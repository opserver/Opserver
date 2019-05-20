using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace StackExchange.Opserver.Helpers
{
    public class BackgroundTaskService : IHostedService
    {
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private Task _backgroundTask;

        public BackgroundTaskService(IBackgroundTaskQueue taskQueue)
        {
            TaskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _backgroundTask = Task.Run(BackgroundProceessing);
            return Task.CompletedTask;
        }

        private async Task BackgroundProceessing()
        {
            while (!_shutdown.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(_shutdown.Token).ConfigureAwait(false);
                try
                {
                    await workItem(_shutdown.Token);
                }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
                catch (Exception) { /* TODO: Log */ }
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _shutdown.Cancel();
            return Task.WhenAny(_backgroundTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }
}
