using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="Task"/> and <see cref="Task{TResult}"/>.
    /// </summary>
    public static class AdapterTaskExtensions {

        /// <summary>
        /// Allows cancellation for a task that doesn't support it. Use this in preference to 
        /// <c>Task.Delay(-1, cancellationToken)</c>, as the latter means that the cancellation 
        /// token registration will never be removed.
        /// </summary>
        /// <typeparam name="T">
        ///   The return type of the inner task.
        /// </typeparam>
        /// <param name="task">
        ///   The task that cannot be cancelled.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token to use.
        /// </param>
        /// <returns>
        ///   A new <see cref="Task{TResult}"/> that will cancel if the <paramref name="cancellationToken"/> 
        ///   fires.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        ///   The <paramref name="cancellationToken"/> fires before the <paramref name="task"/> has 
        ///   completed.
        /// </exception>
        /// <remarks>
        ///   Modified from https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#cancelling-uncancellable-operations
        /// </remarks>
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {
            if (task == null) {
                throw new ArgumentNullException(nameof(task));
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            // This disposes the registration as soon as one of the tasks trigger
            using (cancellationToken.Register(state => {
                ((TaskCompletionSource<object>) state).TrySetResult(null);
            }, tcs)) {
                var resultTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (resultTask == tcs.Task) {
                    // Operation cancelled
                    throw new OperationCanceledException(cancellationToken);
                }

                return await task.ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Allows cancellation for a task that doesn't support it. Use this in preference to 
        /// <c>Task.Delay(-1, cancellationToken)</c>, as the latter means that the cancellation 
        /// token registration will never be removed.
        /// </summary>
        /// <param name="task">
        ///   The task that cannot be cancelled.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token to use.
        /// </param>
        /// <returns>
        ///   A new <see cref="Task"/> that will cancel if the <paramref name="cancellationToken"/> 
        ///   fires.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        ///   The <paramref name="cancellationToken"/> fires before the <paramref name="task"/> has 
        ///   completed.
        /// </exception>
        /// <remarks>
        ///   Modified from https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#cancelling-uncancellable-operations
        /// </remarks>
        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken) {
            if (task == null) {
                throw new ArgumentNullException(nameof(task));
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            // This disposes the registration as soon as one of the tasks trigger
            using (cancellationToken.Register(state => {
                ((TaskCompletionSource<object>) state).TrySetResult(null);
            }, tcs)) {
                var resultTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (resultTask == tcs.Task) {
                    // Operation cancelled
                    throw new OperationCanceledException(cancellationToken);
                }

                await task.ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Creates a new task that cancels if the original task has not completed before the 
        /// specified delay.
        /// </summary>
        /// <typeparam name="T">
        ///   The return type of the task.
        /// </typeparam>
        /// <param name="task">
        ///   The task.
        /// </param>
        /// <param name="delay">
        ///   The cancellation timeout.
        /// </param>
        /// <returns>
        ///   A new task that will cancel if the delay is exceeded.
        /// </returns>
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan delay) {
            if (task == null) {
                throw new ArgumentNullException(nameof(task));
            }

            using (var cts = new CancellationTokenSource(delay)) {
                return await task.WithCancellation(cts.Token).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Creates a new task that cancels if the original task has not completed before the 
        /// specified delay.
        /// </summary>
        /// <param name="task">
        ///   The task.
        /// </param>
        /// <param name="delay">
        ///   The cancellation timeout.
        /// </param>
        /// <returns>
        ///   A new task that will cancel if the delay is exceeded.
        /// </returns>
        public static async Task TimeoutAfter(this Task task, TimeSpan delay) {
            if (task == null) {
                throw new ArgumentNullException(nameof(task));
            }

            using (var cts = new CancellationTokenSource(delay)) {
                await task.WithCancellation(cts.Token).ConfigureAwait(false);
            }
        }

    }

}
