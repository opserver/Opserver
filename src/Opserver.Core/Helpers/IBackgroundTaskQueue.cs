using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Helpers
{
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/dn636893(v=vs.110).aspx
        /// Differs from a normal ThreadPool work item in that ASP.NET can keep track of how many work items registered through
        /// this API are currently running, and the ASP.NET runtime will try to delay AppDomain shutdown until these work items
        /// have finished executing. This API cannot be called outside of an ASP.NET-managed AppDomain. The provided CancellationToken
        /// will be signaled when the application is shutting down.
        /// </summary>
        /// <param name="workItem"></param>
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

        Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);
            return workItem;
        }
    }
}
