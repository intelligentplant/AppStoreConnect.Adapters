using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes an adapter subscription that supports subscribing to/unsubscribing from 
    /// individual topics on the subscription.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of item that is emitted by the subscription.
    /// </typeparam>
    public interface IAdapterSubscriptionWithTopics<T> : IAdapterSubscription<T> {

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
        ValueTask<bool> SubscribeToTopic(string topic);

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
        ValueTask<bool> UnsubscribeFromTopic(string topic);

    }
}
