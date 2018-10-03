using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskCancellation
{
    public class LimitingExecutor : IDisposable
    {
        private const int UpperLimit = 1;
        private readonly Semaphore _semaphore
            = new Semaphore(UpperLimit, UpperLimit);

        public Task Do(Action work, CancellationToken token)
            => Task.Run(() =>
            {
                _semaphore.WaitOne();
                try
                {
                    token.ThrowIfCancellationRequested();
                    work();
                }
                finally
                {
                    _semaphore.Release();
                }
            }, token);

        public void Dispose()
            => _semaphore.Dispose();
    }
}
