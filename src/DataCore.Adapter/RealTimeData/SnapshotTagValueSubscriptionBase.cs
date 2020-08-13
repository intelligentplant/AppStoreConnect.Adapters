using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// A subscription created by a call to <see cref="ISnapshotTagValuePush.Subscribe"/>.
    /// </summary>
    public abstract class SnapshotTagValueSubscriptionBase : AdapterSubscriptionWithTopics<TagValueQueryResult, TagIdentifier>, ISnapshotTagValueSubscription {

        private readonly TimeSpan _publishInterval = TimeSpan.Zero;

        private readonly Dictionary<string, TagValueQueryResult> _pendingValues = new Dictionary<string, TagValueQueryResult>();


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValueSubscriptionBase"/> object.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscription owner.
        /// </param>
        /// <param name="id">
        ///   An identifier for the subscription (e.g. the ID of the adapter that the subscription 
        ///   is being created on). The value does not have to be unique; a fully-qualified 
        ///   identifier will be generated using this value.
        /// </param>
        /// <param name="publishInterval">
        ///   The interval that new values will be published to the subscription at. If less than 
        ///   or equal to <see cref="TimeSpan.Zero"/>, values will be published as soon as they 
        ///   are received by the subscription. See remarks for further details
        /// </param>
        /// <remarks>
        ///   If a positive <paramref name="publishInterval"/> is specified, the subscription will 
        ///   emit new values at every publish interval that have been received since the previous 
        ///   publish interval. Specifying an interval can result in data loss, as only the 
        ///   most-recently received value for each subscribed tag will be emitted; any previous 
        ///   values received during the idle period will be discarded. For example, if a 
        ///   subscription is held for a tag, with a publish interval of 15 seconds, and three 
        ///   values are received before the next publish, only the most-recently received value 
        ///   will be emitted; the other two values are discarded.
        /// </remarks>
        protected SnapshotTagValueSubscriptionBase(
            IAdapterCallContext context, 
            string id, 
            TimeSpan publishInterval
        ) : base(context, id) {
            _publishInterval = publishInterval;
        }


        /// <inheritdoc/>
        protected override async Task Init(CancellationToken cancellationToken) {
            await base.Init(cancellationToken).ConfigureAwait(false);
            if (_publishInterval > TimeSpan.Zero) {
                _ = Task.Run(() => RunPublishLoop(_publishInterval, CancellationToken), CancellationToken);
            }
        }


        /// <inheritdoc/>
        protected sealed override ValueTask<TagIdentifier> ResolveTopic(IAdapterCallContext context, string topic) {
            return ResolveTag(context, topic, CancellationToken);
        }


        /// <inheritdoc/>
        protected sealed override Task OnTopicAdded(TagIdentifier topic) {
            return OnTagAdded(topic);
        }


        /// <inheritdoc/>
        protected sealed override Task OnTopicRemoved(TagIdentifier topic) {
            return OnTagRemoved(topic);
        }


        /// <inheritdoc/>
        protected override string GetTopicNameForValue(TagValueQueryResult value) {
            return value?.TagId;
        }


        /// <inheritdoc/>
        public override bool IsSubscribed(TagValueQueryResult value) {
            if (base.IsSubscribed(value)) {
                // We are subscribed via the tag ID.
                return true;
            }

            // Check if we are subscribed via the tag name instead.
            return IsSubscribedToTopicName(value?.TagName);
        }


        /// <inheritdoc/>
        public override bool IsMatch(TagValueQueryResult value, string topic) {
            if (base.IsMatch(value, topic)) {
                return true;
            }

            if (value == null || string.IsNullOrWhiteSpace(topic)) {
                return false;
            }

            return string.Equals(value?.TagName, topic, System.StringComparison.OrdinalIgnoreCase);
        }


        /// <inheritdoc/>
        public override async ValueTask<bool> ValueReceived(TagValueQueryResult value, CancellationToken cancellationToken = default) {
            if (value == null) {
                return false;
            }

            if (_publishInterval <= TimeSpan.Zero) {
                return await base.ValueReceived(value, cancellationToken).ConfigureAwait(false);
            }

            lock (_pendingValues) {
                _pendingValues[value.TagId] = value;
            }

            return true;
        }


        /// <summary>
        /// Publishes pending values at the specified interval until the provided cancellation token fires.
        /// </summary>
        /// <param name="interval">
        ///   The publish interval.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token to observe.
        /// </param>
        /// <returns>
        ///   A long-running <see cref="Task"/> that will stop when the <paramref name="cancellationToken"/> 
        ///   is cancelled.
        /// </returns>
        private async Task RunPublishLoop(TimeSpan interval, CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    await PublishPendingValues(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    break;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception) { }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }


        /// <summary>
        /// Publishes pending values to channel.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will publish any pending values to the channel.
        /// </returns>
        private async ValueTask PublishPendingValues(CancellationToken cancellationToken) {
            TagValueQueryResult[] values;

            lock (_pendingValues) {
                if (_pendingValues.Count == 0) {
                    return;
                }

                values = _pendingValues.Values.ToArray();
                _pendingValues.Clear();
            }

            foreach (var value in values) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                await WriteToChannel(value, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Invoked when a tag name or ID must be resolved.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the subscriber.
        /// </param>
        /// <param name="tag">
        ///   The tag name or ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the resolved tag identifier, or 
        ///   <see langword="null"/> if the tag cannot be resolved.
        /// </returns>
        protected abstract ValueTask<TagIdentifier> ResolveTag(IAdapterCallContext context, string tag, CancellationToken cancellationToken);


        /// <summary>
        /// Invoked when a tag is added to the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag that was added.
        /// </param>
        /// <returns>
        ///   A task that will perform additional operations associated with the event.
        /// </returns>
        protected abstract Task OnTagAdded(TagIdentifier tag);


        /// <summary>
        /// Invoked when a tag is removed from the subscription.
        /// </summary>
        /// <param name="tag">
        ///   The tag that was removed.
        /// </param>
        /// <returns>
        ///   A task that will perform additional operations associated with the event.
        /// </returns>
        protected abstract Task OnTagRemoved(TagIdentifier tag);

    }
}
