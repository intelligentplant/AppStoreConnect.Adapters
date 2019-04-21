using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Utilities {

    /// <summary>
    /// Base class for simplifying implementation of the <see cref="Features.ISnapshotTagValuePush"/> 
    /// feature.
    /// </summary>
    public abstract class SnapshotTagValueSubscriptionManager : ISnapshotTagValuePush, IDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Fires when then object is being disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Dictionary where the item key is the ID of a tag, and the item value is the total number 
        /// of subscriptions to that tag.
        /// </summary>
        private readonly Dictionary<string, int> _subscriptionCountByTagId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds the current values for subscribed tags.
        /// </summary>
        private readonly ConcurrentDictionary<string, SnapshotTagValue> _currentValueByTagId = new ConcurrentDictionary<string, SnapshotTagValue>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds received value updates that we haven't pushed out yet.
        /// </summary>
        private readonly ConcurrentQueue<IEnumerable<SnapshotTagValue>> _pendingSend = new ConcurrentQueue<IEnumerable<SnapshotTagValue>>();

        /// <summary>
        /// For protecting access to <see cref="_currentValueByTagId"/>.
        /// </summary>
        private readonly SemaphoreSlim _pendingSendAvailable = new SemaphoreSlim(0);

        /// <summary>
        /// A list of all of the current subscriptions.
        /// </summary>
        private readonly HashSet<Subscription> _subscribers = new HashSet<Subscription>();

        /// <summary>
        /// For protecting access to <see cref="_subscriptionCountByTagId"/> and <see cref="_subscribers"/>.
        /// </summary>
        private readonly SemaphoreSlim _subscriptionsLock = new SemaphoreSlim(1, 1);


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValueSubscriptionManager"/> object.
        /// </summary>
        protected SnapshotTagValueSubscriptionManager() {
            _ = Task.Factory.StartNew(() => ProcessSendQueue(_disposedTokenSource.Token), TaskCreationOptions.LongRunning);
        }


        /// <summary>
        /// Adds a subscription for the specified observer.
        /// </summary>
        /// <param name="adapterCallContext">
        ///   The call context for the subscription.
        /// </param>
        /// <param name="channel">
        ///   The channel to write observed values to.
        /// </param>
        /// <returns>
        ///   A subscription object that can be disposed when the subscription is no longer required.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterCallContext"/> is <see langword="null."/>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null."/>
        /// </exception>
        public ISnapshotTagValueSubscription Subscribe(IAdapterCallContext adapterCallContext, ChannelWriter<SnapshotTagValue> channel) {
            if (adapterCallContext == null) {
                throw new ArgumentNullException(nameof(adapterCallContext));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var subscription = new Subscription(this, adapterCallContext, channel);

            _subscriptionsLock.Wait();
            try {
                _subscribers.Add(subscription);
            }
            finally {
                _subscriptionsLock.Release();
            }

            return subscription;
        }


        /// <summary>
        /// Informs the <see cref="SnapshotTagValueSubscriptionManager"/> that a subscription has been 
        /// disposed.
        /// </summary>
        /// <param name="subscription">
        ///   The subscription that was disposed.
        /// </param>
        private void OnSubscriptionDisposed(Subscription subscription) {
            if (_isDisposed) {
                return;
            }

            _subscriptionsLock.Wait();
            try {
                _subscribers.Remove(subscription);
            }
            finally {
                _subscriptionsLock.Release();
            }
        }


        /// <summary>
        /// Gets the IDs of all tags that are currently being subscribed to. Implementers can use this 
        /// method when they need to e.g. reinitialise a real-time push connection to a historian after 
        /// a connection outage.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The IDs of all subscribed tags.
        /// </returns>
        protected async Task<IEnumerable<string>> GetSubscribedTags(CancellationToken cancellationToken) {
            await _subscriptionsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                return _subscriptionCountByTagId.Keys.ToArray();
            }
            finally {
                _subscriptionsLock.Release();
            }
        }


        /// <summary>
        /// Called by a subscription to let the <see cref="SnapshotTagValueSubscriptionManager"/> that 
        /// tags were added to the subscription.
        /// </summary>
        /// <param name="tagIds">
        ///   The tags that were added to the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will register the tag subscriptions and return the initial values for the tags.
        /// </returns>
        private async Task<IEnumerable<SnapshotTagValue>> OnSubscribeInternal(IEnumerable<string> tagIds, CancellationToken cancellationToken) {
            var length = tagIds.Count();
            var newTags = new List<string>(length);
            var initialValues = new List<SnapshotTagValue>(length);

            await _subscriptionsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                foreach (var tagId in tagIds) {
                    if (!_subscriptionCountByTagId.TryGetValue(tagId, out var count)) {
                        newTags.Add(tagId);
                        _subscriptionCountByTagId[tagId] = 1;
                        continue;
                    }
                    _subscriptionCountByTagId[tagId] = count + 1;

                    if (_currentValueByTagId.TryGetValue(tagId, out var value)) {
                        initialValues.Add(value);
                    }
                }

                if (newTags.Count > 0) {
                    var newVals = await OnSubscribe(newTags, cancellationToken).ConfigureAwait(false);
                    initialValues.AddRange(newVals);
                }

                return initialValues;
            }
            finally {
                _subscriptionsLock.Release();
            }
        }


        /// <summary>
        /// Called by a subscription to let the <see cref="SnapshotTagValueSubscriptionManager"/> know 
        /// that tags were removed from the subscription.
        /// </summary>
        /// <param name="tagIds">
        ///   The tags that were removed from the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will unregister the tag subscriptions.
        /// </returns>
        private async Task OnUnsubscribeInternal(IEnumerable<string> tagIds, CancellationToken cancellationToken) {
            var length = tagIds.Count();
            var tagsToRemove = new List<string>(length);

            await _subscriptionsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                foreach (var tagId in tagIds) {
                    if (!_subscriptionCountByTagId.TryGetValue(tagId, out var count)) {
                        continue;
                    }

                    if (count > 1) {
                        _subscriptionCountByTagId[tagId] = count + 1;
                    }
                    else {
                        _subscriptionCountByTagId.Remove(tagId);
                        tagsToRemove.Add(tagId);
                        _currentValueByTagId.TryRemove(tagId, out var _);
                    }
                }

                if (tagsToRemove.Count > 0) {
                    await OnUnsubscribe(tagsToRemove, cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                _subscriptionsLock.Release();
            }
        }


        /// <summary>
        /// Informs the <see cref="SnapshotTagValueSubscriptionManager"/> that snapshot value changes 
        /// have occurred.
        /// </summary>
        /// <param name="values">
        ///   The value changes.
        /// </param>
        protected void OnValuesChanged(IEnumerable<SnapshotTagValue> values) {
            if (values == null || !values.Any()) {
                return;
            }

            var valuesToEmit = new List<SnapshotTagValue>(values.Count());
            foreach (var value in values) {
                if (value == null) {
                    continue;
                }
                if (_disposedTokenSource.IsCancellationRequested) {
                    break;
                }

                if (_currentValueByTagId.TryGetValue(value.TagId, out var previousValue)) {
                    if (previousValue.Value.UtcSampleTime >= value.Value.UtcSampleTime) {
                        continue;
                    }
                }

                _currentValueByTagId[value.TagId] = value;
                valuesToEmit.Add(value);
            }

            if (!_disposedTokenSource.IsCancellationRequested && valuesToEmit.Count > 0) {
                _pendingSend.Enqueue(valuesToEmit);
                _pendingSendAvailable.Release();
            }
        }


        /// <summary>
        /// Long-running task that sends tag values to subscribers whenever they are added to the 
        /// <see cref="_pendingSend"/> queue.
        /// </summary>
        /// <param name="cancellationToken">
        ///   A cancellation token that can be used to stop processing of the queue.
        /// </param>
        /// <returns></returns>
        private async Task ProcessSendQueue(CancellationToken cancellationToken) {
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    await _pendingSendAvailable.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (_pendingSend.TryDequeue(out var values)) {
                        Subscription[] subscribers;

                        await _subscriptionsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try {
                            subscribers = _subscribers.ToArray();
                        }
                        finally {
                            _subscriptionsLock.Release();
                        }

                        if (subscribers.Length == 0) {
                            continue;
                        }

                        await Task.WhenAll(subscribers.Select(x => x.OnValuesChanged(values, cancellationToken))).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) {
                // Cancellation token fired
            }
        }


        /// <summary>
        /// Gets the <see cref="TagIdentifier"/> descriptors for the specified tag names or IDs.
        /// </summary>
        /// <param name="context">
        ///   The adapter call context for the caller.
        /// </param>
        /// <param name="tagNamesOrIds">
        ///   The tag names or IDs to look up.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching <see cref="TagIdentifier"/> object.
        /// </returns>
        protected abstract Task<IEnumerable<TagIdentifier>> GetTags(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken);


        /// <summary>
        /// Called whenever the total subscriber count for a tag changes from zero to greater than zero.
        /// </summary>
        /// <param name="tagIds">
        ///   The tag IDs that have been subscribed to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will perform any additional subscription setup actions required by the 
        ///   adapter and return the initial values of the subscribed tags.
        /// </returns>
        protected abstract Task<IEnumerable<SnapshotTagValue>> OnSubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken);


        /// <summary>
        /// Called whenever the total subscriber count for a tag changes from greater than zero to zero.
        /// </summary>
        /// <param name="tagIds">
        ///   The tag IDs that have been unsubscribed from.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will perform any additional subscription teardown actions required by the 
        ///   adapter.
        /// </returns>
        protected abstract Task OnUnsubscribe(IEnumerable<string> tagIds, CancellationToken cancellationToken);


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (_isDisposed) {
                return;
            }

            DisposeInternal(true);

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Class finalizer.
        /// </summary>
        ~SnapshotTagValueSubscriptionManager() {
            Dispose(false);
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="SnapshotTagValueSubscriptionManager"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        private void DisposeInternal(bool disposing) {
            try {
                Dispose(disposing);
            }
            finally {
                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _subscriptionsLock.Dispose();
                _pendingSendAvailable.Dispose();
            }
        }


        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the <see cref="SnapshotTagValueSubscriptionManager"/> is being 
        ///   disposed, or <see langword="false"/> if it is being finalized.
        /// </param>
        protected abstract void Dispose(bool disposing);


        /// <summary>
        /// Equality comparer for <see cref="TagIdentifier"/> that tests for equality based solely 
        /// on the <see cref="TagIdentifier.Id"/> property.
        /// </summary>
        private class TagIdentifierComparer : IEqualityComparer<TagIdentifier> {

            /// <summary>
            /// Default comparer instance.
            /// </summary>
            public static TagIdentifierComparer Default { get; } = new TagIdentifierComparer();


            /// <summary>
            /// Tests two <see cref="TagIdentifier"/> instances for equality.
            /// </summary>
            /// <param name="x">
            ///   The first instance to compare.
            /// </param>
            /// <param name="y">
            ///   The second instance to compare.
            /// </param>
            /// <returns>
            ///   <see langword="true"/> if the instances are equal, or <see langword="false"/> 
            ///   otherwise.
            /// </returns>
            public bool Equals(TagIdentifier x, TagIdentifier y) {
                if (x == null && y == null) {
                    return true;
                }
                if (x == null || y == null) {
                    return false;
                }

                return string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase);
            }


            /// <summary>
            /// Gets the hash code for the specific <see cref="TagIdentifier"/> instance.
            /// </summary>
            /// <param name="obj">
            ///   The instance.
            /// </param>
            /// <returns>
            ///   The hash code for the instance.
            /// </returns>
            public int GetHashCode(TagIdentifier obj) {
                if (obj == null) {
                    throw new ArgumentNullException(nameof(obj));
                }

                return obj.GetHashCode();
            }
        }


        /// <summary>
        /// <see cref="ISnapshotTagValueSubscription"/> implementation.
        /// </summary>
        private class Subscription : ISnapshotTagValueSubscription {

            /// <summary>
            /// Flags if the object has been disposed.
            /// </summary>
            private bool _isDisposed;

            /// <summary>
            /// Fires when the subscription is disposed.
            /// </summary>
            private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

            /// <summary>
            /// The subscription manager that created the subscription.
            /// </summary>
            private readonly SnapshotTagValueSubscriptionManager _subscriptionManager;

            /// <summary>
            /// The call context to use when adding tags to the subscription.
            /// </summary>
            private readonly IAdapterCallContext _adapterCallContext;

            /// <summary>
            /// The channel writer for the subscription.
            /// </summary>
            private readonly ChannelWriter<SnapshotTagValue> _channel;

            /// <summary>
            /// The tags that have been added to the subscription.
            /// </summary>
            private readonly HashSet<TagIdentifier> _subscribedTags = new HashSet<TagIdentifier>(TagIdentifierComparer.Default);

            /// <summary>
            /// Lock for modifying the subscribed tags.
            /// </summary>
            private readonly SemaphoreSlim _subscriptionLock = new SemaphoreSlim(1, 1);


            /// <summary>
            /// Creates a new <see cref="Subscription"/> object.
            /// </summary>
            /// <param name="subscriptionManager">
            ///   The subscription manager.
            /// </param>
            /// <param name="adapterCallContext">
            ///   The call context to use when adding tags to the subscription.
            /// </param>
            /// <param name="channel">
            ///   The channel writer to send received values to.
            /// </param>
            internal Subscription(SnapshotTagValueSubscriptionManager subscriptionManager, IAdapterCallContext adapterCallContext, ChannelWriter<SnapshotTagValue> channel) {
                _subscriptionManager = subscriptionManager;
                _adapterCallContext = adapterCallContext;
                _channel = channel;
            }


            /// <inheritdoc/>
            public async Task<IEnumerable<TagIdentifier>> GetSubscribedTags(CancellationToken cancellationToken) {
                if (_isDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }

                await _subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    return _subscribedTags.ToArray();
                }
                finally {
                    _subscriptionLock.Release();
                }
            }


            /// <inheritdoc/>
            public async Task<int> AddTagsToSubscription(IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                if (_isDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                if (tagNamesOrIds == null) {
                    throw new ArgumentNullException(nameof(tagNamesOrIds));
                }

                IEnumerable<SnapshotTagValue> initialValues = null;
                var result = 0;

                await _subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    var tagIdentifiers = await _subscriptionManager.GetTags(_adapterCallContext, tagNamesOrIds, cancellationToken).ConfigureAwait(false);
                    var newTags = new List<string>(tagIdentifiers.Count());
                    var dirty = false;

                    if (tagIdentifiers != null) {
                        foreach (var item in tagIdentifiers) {
                            if (item == null) {
                                continue;
                            }
                            var added = _subscribedTags.Add(item);
                            if (added) {
                                dirty = true;
                                newTags.Add(item.Id);
                            }
                        }
                    }

                    if (dirty) {
                        initialValues = await _subscriptionManager.OnSubscribeInternal(newTags, cancellationToken).ConfigureAwait(false);
                    }
                    result = _subscribedTags.Count;
                }
                finally {
                    _subscriptionLock.Release();
                }

                if (initialValues != null) {
                    await OnValuesChanged(initialValues, cancellationToken).ConfigureAwait(false);
                }

                return result;
            }


            /// <inheritdoc/>
            public async Task<int> RemoveTagsFromSubscription(IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                if (_isDisposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                if (tagNamesOrIds == null) {
                    throw new ArgumentNullException(nameof(tagNamesOrIds));
                }

                await _subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    var tagsToRemove = new List<string>(tagNamesOrIds.Count());

                    foreach (var item in tagNamesOrIds) {
                        if (item == null) {
                            continue;
                        }

                        var tagIdentifier = _subscribedTags.FirstOrDefault(x => string.Equals(item, x.Id, StringComparison.OrdinalIgnoreCase) || string.Equals(item, x.Name, StringComparison.OrdinalIgnoreCase));
                        if (tagIdentifier == null) {
                            continue;
                        }

                        tagsToRemove.Add(tagIdentifier.Id);
                        _subscribedTags.Remove(tagIdentifier);
                    }

                    if (tagsToRemove.Count > 0) {
                        await _subscriptionManager.OnUnsubscribeInternal(tagsToRemove, cancellationToken).ConfigureAwait(false);
                    }
                    return _subscribedTags.Count;
                }
                finally {
                    _subscriptionLock.Release();
                }
            }


            /// <summary>
            /// Sends value changes to the observer.
            /// </summary>
            /// <param name="values">
            ///   The updated values.
            /// </param>
            /// <param name="cancellationToken">
            ///   The cancellation token for the operation.
            /// </param>
            /// <returns>
            ///   A task that will filter out value changes that are not relevant to this subscription, 
            ///   and then forward the remaining values to the observer.
            /// </returns>
            internal async Task OnValuesChanged(IEnumerable<SnapshotTagValue> values, CancellationToken cancellationToken) {
                if (_isDisposed) {
                    return;
                }

                SnapshotTagValue[] valuesToEmit;

                await _subscriptionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try {
                    valuesToEmit = values
                        .ToLookup(x => x.TagId, StringComparer.OrdinalIgnoreCase)
                        .Where(grp => _subscribedTags.Any(t => t.Id.Equals(grp.Key, StringComparison.OrdinalIgnoreCase)))
                        .SelectMany(grp => grp)
                        .OrderBy(x => x.Value.UtcSampleTime)
                        .ToArray();
                }
                finally {
                    _subscriptionLock.Release();
                }

                if (valuesToEmit.Length == 0) {
                    return;
                }

                _ = WriteValuesToChannel(valuesToEmit, _disposedTokenSource.Token);
            }


            /// <summary>
            /// Writes the specified values to the channel writer.
            /// </summary>
            /// <param name="values">
            ///   The values to write.
            /// </param>
            /// <param name="cancellationToken">
            ///   The cancellation token for the operation.
            /// </param>
            /// <returns>
            ///   A task that will write the values.
            /// </returns>
            private async Task WriteValuesToChannel(IEnumerable<SnapshotTagValue> values, CancellationToken cancellationToken) {
                if (_isDisposed) {
                    return;
                }

                try {
                    foreach (var value in values) {
                        if (_isDisposed || cancellationToken.IsCancellationRequested) {
                            break;
                        }
                        await _channel.WriteAsync(value, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception) {
                    // Do nothing
                }
            }


            /// <summary>
            /// Disposes of the subscription.
            /// </summary>
            public void Dispose() {
                if (_isDisposed) {
                    return;
                }

                _disposedTokenSource.Cancel();
                _disposedTokenSource.Dispose();
                _subscriptionManager.OnSubscriptionDisposed(this);
                _subscriptionLock.Dispose();
                _isDisposed = true;
            }
        }

    }
}
