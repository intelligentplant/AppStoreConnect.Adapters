using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for requesting tag values, including pushing real-time snapshot value 
    // changes to subscribers. Snapshot push is only supported on adapters that implement the 
    // ISnapshotTagValuePush feature.

    public partial class AdapterHub {

        #region [ OnConnected/OnDisconnected ]

        /// <summary>
        /// Invoked when a new connection is created.
        /// </summary>
        private void OnTagValuesHubConnected() {
            // Store a list of adapter subscriptions in the connection context.
            Context.Items[typeof(ISnapshotTagValueSubscription)] = new List<SubscriptionWrapper>();
        }


        /// <summary>
        /// Invoked when a connection is closed.
        /// </summary>
        private void OnTagValuesHubDisconnected() {
            // Remove the adapter subscriptions from the connection context.
            if (Context.Items.TryGetValue(typeof(ISnapshotTagValueSubscription), out var o)) {
                Context.Items.Remove(typeof(ISnapshotTagValueSubscription));
                if (o is List<SubscriptionWrapper> observers) {
                    lock (observers) {
                        foreach (var observer in observers.ToArray()) {
                            observer.Dispose();
                        }
                        observers.Clear();
                    }
                }
            }
        }

        #endregion

        #region [ Snapshot Subscription Management ]

        /// <summary>
        /// Creates a new snapshot push subscription on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="tags">
        ///   The tags to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive new tag values.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannel(string adapterId, IEnumerable<string> tags, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterId, cancellationToken).ConfigureAwait(false);
            var subscription = await GetOrCreateSnapshotSubscription(AdapterCallContext, adapter.Adapter, adapter.Feature, cancellationToken).ConfigureAwait(false);

            tags = tags
                ?.Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (tags.Any()) {
                await subscription.AddTagsToSubscription(AdapterCallContext, tags, cancellationToken).ConfigureAwait(false);
            }

            return subscription.Reader;
        }


        /// <summary>
        /// Gets the tags that an existing snapshot subscription is subscribed to. You must create a 
        /// channel first by calling <see cref="CreateSnapshotTagValueChannel(string, IEnumerable{string}, CancellationToken)"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID for the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that the caller can subscribe to to receive the tags that the 
        ///   subscription holds references to.
        /// </returns>
        public async Task<ChannelReader<TagIdentifier>> GetSnapshotTagValueChannelSubscriptions(string adapterId, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterId, cancellationToken).ConfigureAwait(false);
            var subscription = GetSnapshotSubscription(adapter.Adapter);
            if (subscription == null) {
                throw new ArgumentException(Resources.Error_AdapterSubscriptionDoesNotExist, nameof(adapterId));
            }

            return subscription.GetTags(AdapterCallContext, cancellationToken);
        }


        /// <summary>
        /// Adds tags to an existing snapshot data channel subscription. You must create a channel 
        /// first by calling <see cref="CreateSnapshotTagValueChannel(string, IEnumerable{string}, CancellationToken)"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID for the channel to modify.
        /// </param>
        /// <param name="tags">
        ///   The tags to add to the subscription.
        /// </param>
        /// <returns>
        ///   The total number of tags in the subscription.
        /// </returns>
        public async Task<int> AddTagsToSnapshotTagValueChannel(string adapterId, IEnumerable<string> tags) {
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            tags = tags
                ?.Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? new string[0];

            if (!tags.Any()) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagIsRequired, nameof(tags));
            }

            var subscription = GetSnapshotSubscription(adapter.Adapter);
            if (subscription == null) {
                throw new ArgumentException(Resources.Error_AdapterSubscriptionDoesNotExist, nameof(adapterId));
            }

            return await subscription.AddTagsToSubscription(AdapterCallContext, tags, Context.ConnectionAborted).ConfigureAwait(false);
        }


        /// <summary>
        /// Removes tags from an existing snapshot data channel subscription. You must create a channel 
        /// first by calling <see cref="CreateSnapshotTagValueChannel(string, IEnumerable{string}, CancellationToken)"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID for the channel to modify.
        /// </param>
        /// <param name="tags">
        ///   The tags to remove from the subscription.
        /// </param>
        /// <returns>
        ///   The total number of tags in the subscription.
        /// </returns>
        public async Task<int> RemoveTagsFromSnapshotTagValueChannel(string adapterId, IEnumerable<string> tags) {
            var adapter = await ResolveAdapterAndFeature<ISnapshotTagValuePush>(adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            tags = tags
                ?.Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? new string[0];

            if (!tags.Any()) {
                throw new ArgumentException(Resources.Error_AtLeastOneTagIsRequired, nameof(tags));
            }

            var subscription = GetSnapshotSubscription(adapter.Adapter);
            if (subscription == null) {
                throw new ArgumentException(Resources.Error_AdapterSubscriptionDoesNotExist, nameof(adapterId));
            }

            return await subscription.RemoveTagsFromSubscription(AdapterCallContext, tags, Context.ConnectionAborted).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets or creates a real-time data subscription on the specified adapter.
        /// </summary>
        /// <param name="callContext">
        ///   The call context.
        /// </param>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="feature">
        ///   The snapshot push feature for the adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token that will fire when the subscription is no longer required.
        /// </param>
        /// <returns>
        ///   An <see cref="ISnapshotTagValueSubscription"/> for the specified adapter.
        /// </returns>
        private async Task<ISnapshotTagValueSubscription> GetOrCreateSnapshotSubscription(IAdapterCallContext callContext, IAdapter adapter, ISnapshotTagValuePush feature, CancellationToken cancellationToken) {
            var subscription = GetSnapshotSubscription(adapter);
            if (subscription != null) {
                return subscription;
            }

            var subscriptionsForConnection = Context.Items[typeof(ISnapshotTagValueSubscription)] as List<SubscriptionWrapper>;
            subscription = await feature.Subscribe(callContext, cancellationToken).ConfigureAwait(false);

            SubscriptionWrapper result;
            lock (subscriptionsForConnection) {
                result = new SubscriptionWrapper(adapter.Descriptor.Id, subscription, subscriptionsForConnection, cancellationToken);
                subscriptionsForConnection.Add(result);
            }

            await result.StartAsync(callContext, cancellationToken).ConfigureAwait(false);
            return result;
        }


        /// <summary>
        /// Gets an existing real-time data subscription on the specified adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The <see cref="ISnapshotTagValueSubscription"/> for the specified adapter, or 
        ///   <see langword="null"/> if a subscription does not exist.
        /// </returns>
        private ISnapshotTagValueSubscription GetSnapshotSubscription(IAdapter adapter) {
            var subscriptionsForConnection = Context.Items[typeof(ISnapshotTagValueSubscription)] as List<SubscriptionWrapper>;
            return subscriptionsForConnection?.FirstOrDefault(x => string.Equals(x.AdapterId, adapter.Descriptor.Id));
        }

        #endregion

        #region [ Polling Data Queries ]

        /// <summary>
        /// Gets snapshot tag values via polling. Use <see cref="CreateSnapshotTagValueChannel(string, IEnumerable{string}, CancellationToken)"/> 
        /// to receive snapshot tag values via push messages.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValues(string adapterId, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadSnapshotTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadSnapshotTagValues(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets raw tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadRawTagValues(string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadRawTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadRawTagValues(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets visualization-friendly tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadPlotTagValues(string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadPlotTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadPlotTagValues(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets interpolated tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadInterpolatedTagValues(string adapterId, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadInterpolatedTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadInterpolatedTagValues(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets tag values at the specified times.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<TagValueQueryResult>> ReadTagValuesAtTimes(string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadTagValuesAtTimes>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadTagValuesAtTimes(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets the data functions supported by <see cref="ReadProcessedTagValues(string, ReadProcessedTagValuesRequest, CancellationToken)"/> 
        /// queries.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The supported data functions for processed data queries.
        /// </returns>
        public async Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctions(string adapterId, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            return await adapter.Feature.GetSupportedDataFunctions(AdapterCallContext, cancellationToken).ConfigureAwait(false); ;
        }


        /// <summary>
        /// Gets processed tag values.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   A cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the query results.
        /// </returns>
        public async Task<ChannelReader<ProcessedTagValueQueryResult>> ReadProcessedTagValues(string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadProcessedTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadProcessedTagValues(AdapterCallContext, request, cancellationToken);
        }

        #endregion

        #region [ Tag Value Write ]

        /// <summary>
        /// Writes values to the specified adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="channel">
        ///   A channel that will provide the values to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the write results.
        /// </returns>
        public async Task<ChannelReader<WriteTagValueResult>> WriteSnapshotTagValues(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IWriteSnapshotTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            return adapter.Feature.WriteSnapshotTagValues(AdapterCallContext, channel, cancellationToken);
        }


        /// <summary>
        /// Writes values to the specified adapter's historical archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="channel">
        ///   A channel that will provide the values to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that will return the write results.
        /// </returns>
        public async Task<ChannelReader<WriteTagValueResult>> WriteHistoricalTagValues(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IWriteHistoricalTagValues>(adapterId, cancellationToken).ConfigureAwait(false);
            return adapter.Feature.WriteHistoricalTagValues(AdapterCallContext, channel, cancellationToken);
        }

        #endregion

        #region [ Inner Types ]

        /// <summary>
        /// Wrapper for <see cref="ISnapshotTagValueSubscription"/> to ensure that the inner subscription is 
        /// cancelled if a client disconnects or stops the stream. 
        /// </summary>
        private class SubscriptionWrapper : ISnapshotTagValueSubscription {

            public string AdapterId { get; }

            private readonly ISnapshotTagValueSubscription _inner;

            private Action _onDisposed;

            private readonly CancellationTokenRegistration _onStreamCancelled;

            /// <inheritdoc/>
            public bool IsStarted {
                get { return _inner.IsStarted; }
            }

            /// <inheritdoc/>
            public ChannelReader<TagValueQueryResult> Reader { get { return _inner.Reader; } }

            /// <inheritdoc/>
            public int Count { get { return _inner.Count; } }


            public SubscriptionWrapper(string adapterId, ISnapshotTagValueSubscription inner, ICollection<SubscriptionWrapper> subscriptionsForConnection, CancellationToken streamCancelled) {
                AdapterId = adapterId ?? throw new ArgumentNullException(nameof(adapterId));
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
                _onDisposed = () => {
                    lock (subscriptionsForConnection) {
                        subscriptionsForConnection.Remove(this);
                    }
                };
                _onStreamCancelled = streamCancelled.Register(Dispose);
            }


            /// <inheritdoc/>
            public async ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
                await _inner.StartAsync(context, cancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            public ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken) {
                return _inner.GetTags(context, cancellationToken);
            }


            /// <inheritdoc/>
            public Task<int> AddTagsToSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                return _inner.AddTagsToSubscription(context, tagNamesOrIds, cancellationToken);
            }


            /// <inheritdoc/>
            public Task<int> RemoveTagsFromSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                return _inner.RemoveTagsFromSubscription(context, tagNamesOrIds, cancellationToken);
            }


            /// <inheritdoc/>
            public void Dispose() {
                _onDisposed?.Invoke();
                _onDisposed = null;
                _onStreamCancelled.Dispose();
                _inner.Dispose();
            }


            /// <inheritdoc/>
            public async ValueTask DisposeAsync() {
                _onDisposed?.Invoke();
                _onDisposed = null;
                _onStreamCancelled.Dispose();
                await _inner.DisposeAsync().ConfigureAwait(false);
            }

        }

        #endregion

    }
}
