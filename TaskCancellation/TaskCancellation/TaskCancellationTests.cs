using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TaskCancellation
{
    public class TaskCancellationTests
    {
        [Test]
        public void RequestPropagationTest()
        {
            using (var setupEvent = new ManualResetEvent(initialState: false))
            using (var cancellation = new CancellationTokenSource())
            using (var executor = new LimitingExecutor())
            {
                // System-state setup action:
                var cancellingTask = executor.Do(() =>
                {
                    setupEvent.WaitOne();
                    cancellation.Cancel();
                }, CancellationToken.None);

                // "Simulates" some other load on Thread Pool while this test
                // is running, like other tests executed in sibling AppDomains:
                Task.Run(() => Thread.Sleep(millisecondsTimeout: 123));

                // Main work action:
                var actionTask = executor.Do(() =>
                {
                    throw new InvalidOperationException(
                        "This action should be cancelled!");
                }, cancellation.Token);

                // Let's unblock slot in Executor for the 'main work action'
                // by finalizing the 'system-state setup action' which will
                // finally request "global" cancellation:
                setupEvent.Set();

                Assert.DoesNotThrowAsync(
                    async () => await cancellingTask);

                Assert.ThrowsAsync<TaskCanceledException>(
                    async () => await actionTask);
            }
        }
    }
}
