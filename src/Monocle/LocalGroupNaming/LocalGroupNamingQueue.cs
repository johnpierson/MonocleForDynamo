using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonocleViewExtension.LocalGroupNaming
{
    internal sealed class LocalGroupNamingQueue : IDisposable
    {
        private readonly Func<LocalGroupNamingRequest, CancellationToken, Task> processRequest;
        private readonly Action<LocalGroupNamingRequest, Exception> handleError;
        private readonly object syncRoot = new object();
        private readonly Queue<LocalGroupNamingRequest> pendingRequests = new Queue<LocalGroupNamingRequest>();
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private Task processingTask = Task.CompletedTask;
        private bool disposed;

        public LocalGroupNamingQueue(
            Func<LocalGroupNamingRequest, CancellationToken, Task> processRequest,
            Action<LocalGroupNamingRequest, Exception> handleError)
        {
            this.processRequest = processRequest ?? throw new ArgumentNullException(nameof(processRequest));
            this.handleError = handleError ?? throw new ArgumentNullException(nameof(handleError));
        }

        public Task WhenIdle
        {
            get
            {
                lock (syncRoot)
                {
                    return processingTask;
                }
            }
        }

        public bool Enqueue(LocalGroupNamingRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            lock (syncRoot)
            {
                if (disposed || cancellation.IsCancellationRequested) return false;

                pendingRequests.Enqueue(request);
                if (processingTask.IsCompleted)
                {
                    processingTask = ProcessRequestsAsync();
                }
            }

            return true;
        }

        public void Cancel()
        {
            lock (syncRoot)
            {
                pendingRequests.Clear();
            }

            cancellation.Cancel();
        }

        private async Task ProcessRequestsAsync()
        {
            while (true)
            {
                LocalGroupNamingRequest request;
                lock (syncRoot)
                {
                    if (pendingRequests.Count == 0)
                    {
                        processingTask = Task.CompletedTask;
                        return;
                    }

                    request = pendingRequests.Dequeue();
                }

                try
                {
                    cancellation.Token.ThrowIfCancellationRequested();
                    await processRequest(request, cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    // One failed request must not strand the groups behind it.
                    handleError(request, exception);
                }
            }
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed) return;
                disposed = true;
                pendingRequests.Clear();
            }

            cancellation.Cancel();
        }
    }
}
