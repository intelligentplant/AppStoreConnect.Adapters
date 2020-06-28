using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Manages subscriptions on behalf of a persistent connection.
    /// </summary>
    /// <typeparam name="TVal">
    ///   The value type for the subscription.
    /// </typeparam>
    /// <typeparam name="TSub">
    ///   The subscription type.
    /// </typeparam>
    public class ConnectionSubscriptionManager<TVal, TSub> where TSub : IAdapterSubscription<TVal> {

        /// <summary>
        /// Subscriptions indexed by connection ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, ConnectionSubscriptionList<TVal, TSub>> _subscriptions = new ConcurrentDictionary<string, ConnectionSubscriptionList<TVal, TSub>>();


        /// <summary>
        /// Gets the connection IDs that are registered with the subscription manager.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetConnectionIds() {
            return _subscriptions.Keys.ToArray();
        }


        /// <summary>
        /// Adds a subscription.
        /// </summary>
        /// <param name="connectionId">
        ///   The connection ID.
        /// </param>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <returns>
        ///   A unique identifier for the subscription.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="connectionId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="subscription"/> is <see langword="null"/>.
        /// </exception>
        public string AddSubscription(string connectionId, TSub subscription) {
            if (string.IsNullOrWhiteSpace(connectionId)) {
                throw new ArgumentException(Resources.Error_ConnectionIdRequired, nameof(connectionId));
            }
            if (subscription == null) {
                throw new ArgumentNullException(nameof(subscription));
            }
            var subscriptions = _subscriptions.GetOrAdd(connectionId, k => new ConnectionSubscriptionList<TVal, TSub>(k));
            return subscriptions.Add(subscription);
        }


        /// <summary>
        /// Removes and disposes of a subscription.
        /// </summary>
        /// <param name="connectionId">
        ///   The connection ID.
        /// </param>
        /// <param name="subscriptionId">
        ///   The unique identifier for the subscription.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription was removed, or <see langword="false"/> 
        ///   if the subscription was not found.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="connectionId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="subscriptionId"/> is <see langword="null"/> or white space.
        /// </exception>
        public bool RemoveSubscription(string connectionId, string subscriptionId) {
            if (string.IsNullOrWhiteSpace(connectionId)) {
                throw new ArgumentException(Resources.Error_ConnectionIdRequired, nameof(connectionId));
            }
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentException(Resources.Error_SubscriptionIdRequired, nameof(subscriptionId));
            }

            if (!_subscriptions.TryGetValue(connectionId, out var subscriptions)) {
                return false;
            }

            return subscriptions.Remove(subscriptionId);
        }


        /// <summary>
        /// Removes and disposes of all subscriptions for the specified connection.
        /// </summary>
        /// <param name="connectionId">
        ///   The connection ID.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="connectionId"/> is <see langword="null"/> or white space.
        /// </exception>
        public void RemoveAllSubscriptions(string connectionId) {
            if (string.IsNullOrWhiteSpace(connectionId)) {
                throw new ArgumentException(Resources.Error_ConnectionIdRequired, nameof(connectionId));
            }

            if (!_subscriptions.TryRemove(connectionId, out var subscriptions)) {
                return;
            }

            subscriptions.Dispose();
        }


        /// <summary>
        /// Tries to get the specified subscription.
        /// </summary>
        /// <param name="connectionId">
        ///   The connection ID.
        /// </param>
        /// <param name="subscriptionId">
        ///   The subscription ID.
        /// </param>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription was found, or <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="connectionId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="subscriptionId"/> is <see langword="null"/> or white space.
        /// </exception>
        public bool TryGetSubscription(string connectionId, string subscriptionId, out TSub subscription) {
            if (string.IsNullOrWhiteSpace(connectionId)) {
                throw new ArgumentException(Resources.Error_ConnectionIdRequired, nameof(connectionId));
            }
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentException(Resources.Error_SubscriptionIdRequired, nameof(subscriptionId));
            }

            if (!_subscriptions.TryGetValue(connectionId, out var subscriptions)) {
                subscription = default;
                return false;
            }

            return subscriptions.TryGet(subscriptionId, out subscription);
        }


        /// <summary>
        /// Updates the heartbeat time for the specified connection.
        /// </summary>
        /// <param name="connectionId">
        ///   The connection ID.
        /// </param>
        /// <param name="utcHeartbeatTime">
        ///   The UTC heartbeat time.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="connectionId"/> is <see langword="null"/> or white space.
        /// </exception>
        public void SetHeartbeat(string connectionId, DateTime utcHeartbeatTime) {
            if (string.IsNullOrWhiteSpace(connectionId)) {
                throw new ArgumentException(Resources.Error_ConnectionIdRequired, nameof(connectionId));
            }

            if (!_subscriptions.TryGetValue(connectionId, out var subscriptions)) {
                return;
            }

            subscriptions.UtcLastClientHeartbeat = utcHeartbeatTime.ToUniversalTime();
        }


        /// <summary>
        /// Checks to see if the heartbeat time for the specfied connection was further in the 
        /// past than the specified timeout.
        /// </summary>
        /// <param name="connectionId">
        ///   The connection ID.
        /// </param>
        /// <param name="timeout">
        ///   The timeout.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the connection has gone stale, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public bool IsConnectionStale(string connectionId, TimeSpan timeout) {
            if (string.IsNullOrWhiteSpace(connectionId)) {
                throw new ArgumentException(Resources.Error_ConnectionIdRequired, nameof(connectionId));
            }

            if (!_subscriptions.TryGetValue(connectionId, out var subscriptions)) {
                return false;
            }

            return (DateTime.UtcNow - subscriptions.UtcLastClientHeartbeat) > timeout;
        }

    }


    /// <summary>
    /// Holds subscriptions for a single connection.
    /// </summary>
    /// <typeparam name="TVal">
    ///   The value type for the subscription.
    /// </typeparam>
    /// <typeparam name="TSub">
    ///   The subscription type.
    /// </typeparam>
    public sealed class ConnectionSubscriptionList<TVal, TSub> : IDisposable where TSub : IAdapterSubscription<TVal> {

        /// <summary>
        /// The subscriptions.
        /// </summary>
        private readonly ConcurrentDictionary<string, TSub> _subscriptions = new ConcurrentDictionary<string, TSub>();

        /// <summary>
        /// The connection ID.
        /// </summary>
        internal string ConnectionId { get; }

        /// <summary>
        /// The last time that a heartbeat message was received by the connection. This can be 
        /// used to clean up subscriptions for connections that become stale.
        /// </summary>
        internal DateTime UtcLastClientHeartbeat { get; set; }


        /// <summary>
        /// Creates a new <see cref="ConnectionSubscriptionList{TVal, TSub}"/> object.
        /// </summary>
        /// <param name="connectionId">
        ///   The connection ID.
        /// </param>
        internal ConnectionSubscriptionList(string connectionId) {
            ConnectionId = connectionId;
            UtcLastClientHeartbeat = DateTime.UtcNow;
        }


        /// <summary>
        /// Adds a subscription.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <returns>
        ///   The subscription ID.
        /// </returns>
        internal string Add(TSub subscription) {
            _subscriptions[subscription.Id] = subscription;
            return subscription.Id;
        }


        /// <summary>
        /// Removes and disposes of a subscription.
        /// </summary>
        /// <param name="id">
        ///   The subscription ID.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription was removed, or <see langword="false"/> 
        ///   if the subscription was not found.
        /// </returns>
        internal bool Remove(string id) {
            if (_subscriptions.TryRemove(id, out var sub)) {
                sub.Dispose();
                return true;
            }

            return false;
        }


        /// <summary>
        /// Tries to get a subscription.
        /// </summary>
        /// <param name="id">
        ///   The subscription ID.
        /// </param>
        /// <param name="subscription">
        ///   The subscription.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the subscription was found, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        internal bool TryGet(string id, out TSub subscription) {
            if (_subscriptions.TryGetValue(id, out subscription)) {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Disposes of all subscriptions for the connection.
        /// </summary>
        public void Dispose() {
            foreach (var item in _subscriptions.Values) {
                item.Dispose();
            }

            _subscriptions.Clear();
        }

    }




}
