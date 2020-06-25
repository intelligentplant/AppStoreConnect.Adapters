using System;
using System.Threading.Tasks;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Represents a subscription change request in <see cref="AdapterSubscriptionWithTopics{TValue, TTopic}"/>.
    /// </summary>
    public class SubscriptionTopicChange {

        /// <summary>
        /// The change request.
        /// </summary>
        public UpdateSubscriptionTopicsRequest Request { get; }

        /// <summary>
        /// A <see cref="TaskCompletionSource{TResult}"/> that will return a flag indicating if 
        /// the change was successfully applied.
        /// </summary>
        private readonly TaskCompletionSource<bool> _complete = new TaskCompletionSource<bool>();

        /// <summary>
        /// A <see cref="Task{TResult}"/> that will return a flag indicating if the change was 
        /// successfully applied.
        /// </summary>
        public Task<bool> Completed => _complete.Task;


        /// <summary>
        /// Creates a new <see cref="SubscriptionTopicChange"/>.
        /// </summary>
        /// <param name="request">
        ///   The subscription change request.
        /// </param>
        internal SubscriptionTopicChange(UpdateSubscriptionTopicsRequest request) {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }


        /// <summary>
        /// Sets the result of the <see cref="Completed"/> task.
        /// </summary>
        /// <param name="success">
        ///   The completed status.
        /// </param>
        public void SetResult(bool success) {
            _complete.TrySetResult(success);
        }

    }
}
