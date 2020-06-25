using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Base implementation of <see cref="IAdapterSubscriptionWithTopics{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of item that is emitted by the subscription.
    /// </typeparam>
    public abstract class AdapterSubscriptionWithTopics<T> : AdapterSubscription<T>, IAdapterSubscriptionWithTopics<T> {

        /// <summary>
        /// Creates a new <see cref="AdapterSubscription{T}"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        /// <param name="id">
        ///   An identifier for the subscription (e.g. the ID of the adapter that the subscription 
        ///   is being created on). The value does not have to be unique; a fully-qualified 
        ///   identifier will be generated using this value.
        /// </param>
        protected AdapterSubscriptionWithTopics(IAdapterCallContext context, string id) 
            : base(context, id) { }


        /// <inheritdoc/>
        async ValueTask<bool> IAdapterSubscriptionWithTopics<T>.SubscribeToTopic(string topic) {
            if (CancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(topic)) {
                return false;
            }

            return await SubscribeToTopic(topic).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        async ValueTask<bool> IAdapterSubscriptionWithTopics<T>.UnsubscribeFromTopic(string topic) {
            if (CancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(topic)) {
                return false;
            }

            return await UnsubscribeFromTopic(topic).ConfigureAwait(false);
        }


        /// <summary>
        /// Subscribes to the specified topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        protected abstract ValueTask<bool> SubscribeToTopic(string topic);


        /// <summary>
        /// Unsubscribes from the specified topic.
        /// </summary>
        /// <param name="topic">
        ///   The topic.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a <see cref="bool"/> indicating 
        ///   if the operation was successful.
        /// </returns>
        protected abstract ValueTask<bool> UnsubscribeFromTopic(string topic);

    }
}
