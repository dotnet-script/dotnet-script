using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dotnet.Script.LanguageServices
{
    internal static class BoundedOperation
    {
        /// <summary>
        /// Runs an asynchronous Roslyn query from the synchronous editor loop. A key stroke must never
        /// block on a cold compilation, so the wait is bounded and gives up on the fallback value.
        /// </summary>
        public static T Run<T>(Func<CancellationToken, Task<T>> operation, TimeSpan timeout, T fallback)
        {
            var cancellation = new CancellationTokenSource();

            try
            {
                var task = Task.Run(() => operation(cancellation.Token), cancellation.Token);

                if (task.Wait(timeout))
                {
                    cancellation.Dispose();
                    return task.Result;
                }

                cancellation.Cancel();

                // The abandoned task still holds the token and may fault; both are settled once it ends.
                task.ContinueWith(
                    abandoned =>
                    {
                        _ = abandoned.Exception;
                        cancellation.Dispose();
                    },
                    TaskScheduler.Default);

                return fallback;
            }
            catch (Exception)
            {
                cancellation.Dispose();
                return fallback;
            }
        }
    }
}
