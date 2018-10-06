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

                    // The "easiest solution" - just wait a little bit.
                    // However, this test still could be unstable,
                    // and often is unnecessary long to execute.
                    Thread.Sleep(millisecondsTimeout: 3333);
                }, CancellationToken.None);

                // Main work action:
                var actionTask = executor.Do(() =>
                {
                    throw new InvalidOperationException(
                        "This action should be cancelled!");
                }, cancellation.Token);

                // Let's wait until this `Task` starts, so it will got opportunity
                // to cancel itself, and expected later exception will not come
                // from just starting that action by `Task.Run` with token:
                while (actionTask.Status < TaskStatus.Running)
                    Thread.Sleep(millisecondsTimeout: 1);

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
