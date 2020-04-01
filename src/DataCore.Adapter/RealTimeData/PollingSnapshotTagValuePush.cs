using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extends <see cref="SnapshotTagValuePush"/> to implement pseudo-push capabilities 
    /// for an adapter that does not natively support tag value push by polling for new values on
    /// a periodic basis.
    /// </summary>
    public class PollingSnapshotTagValuePush : SnapshotTagValuePush {

        /// <summary>
        /// The feature that provides the snapshot tag values.
        /// </summary>
        private readonly IReadSnapshotTagValues _readSnapshotFeature;

        /// <summary>
        /// Default polling interval to use.
        /// </summary>
        public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// The polling interval.
        /// </summary>
        private readonly TimeSpan _pollingInterval;

        /// <summary>
        /// All subscribed tags.
        /// </summary>
        private readonly List<TagIdentifier> _subscribedTags = new List<TagIdentifier>();


        /// <summary>
        /// Creates a new <see cref="PollingSnapshotTagValuePush"/> object.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="tagInfoFeature">
        ///   An <see cref="ITagInfo"/> adapter feature that will be used to resolve tag 
        ///   identifiers.
        /// </param>
        /// <param name="readSnapshotFeature">
        ///   An <see cref="IReadSnapshotTagValues"/> adapter feature that will be used to poll for 
        ///   new values on a periodic basis.
        /// </param>
        /// <param name="pollingInterval">
        ///   The polling interval to use. If the value specified is less than or equal to 
        ///   <see cref="TimeSpan.Zero"/>, <see cref="DefaultPollingInterval"/> will be used.
        /// </param>
        /// <param name="scheduler">
        ///   The scheduler to use when running background tasks. If the value specified is 
        ///   <see langword="null"/>, <see cref="BackgroundTaskService.Default"/> will be used.
        /// </param>
        /// <param name="logger">
        ///   The logger to use. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagInfoFeature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="readSnapshotFeature"/> is <see langword="null"/>.
        /// </exception>
        private PollingSnapshotTagValuePush(
            string adapterId,
            ITagInfo tagInfoFeature, 
            IReadSnapshotTagValues readSnapshotFeature, 
            TimeSpan pollingInterval,
            IBackgroundTaskService scheduler,
            ILogger logger
        ) : base(
            new SnapshotTagValuePushOptions() { 
                AdapterId = adapterId,
                TagResolver = SnapshotTagValuePushOptions.CreateTagResolver(tagInfoFeature)
            }, 
            scheduler,
            logger
        ) {
            _readSnapshotFeature = readSnapshotFeature ?? throw new ArgumentNullException(nameof(readSnapshotFeature));
            _pollingInterval = pollingInterval > TimeSpan.Zero
                ? pollingInterval
                : DefaultPollingInterval;

            Scheduler.QueueBackgroundWorkItem(RunSnapshotPollingLoop, null, DisposedToken);
        }


        /// <summary>
        /// Creates a new <see cref="PollingSnapshotTagValuePush"/> object for the specified 
        /// adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="pollingInterval">
        ///   The polling interval to use when refreshing values for subscribed tags.
        /// </param>
        /// <returns>
        ///   A new <see cref="PollingSnapshotTagValuePush"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapter"/> does not meet the requirements specified by 
        ///   <see cref="IsCompatible"/>.
        /// </exception>
        public static PollingSnapshotTagValuePush ForAdapter(
            AdapterBase adapter,
            TimeSpan pollingInterval
        ) {
            return ForAdapter(adapter, pollingInterval, adapter?.TaskScheduler, adapter?.Logger);
        }


        /// <summary>
        /// Creates a new <see cref="PollingSnapshotTagValuePush"/> object for the specified 
        /// adapter.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="pollingInterval">
        ///   The polling interval to use when refreshing values for subscribed tags.
        /// </param>
        /// <param name="scheduler">
        ///   The scheduler to use when running background tasks. If the value specified is 
        ///   <see langword="null"/>, <see cref="BackgroundTaskService.Default"/> will be used.
        /// </param>
        /// <param name="logger">
        ///   The logger to use. Can be <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="PollingSnapshotTagValuePush"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapter"/> does not meet the requirements specified by 
        ///   <see cref="IsCompatible"/>.
        /// </exception>
        public static PollingSnapshotTagValuePush ForAdapter(
            IAdapter adapter,
            TimeSpan pollingInterval,
            IBackgroundTaskService scheduler = null,
            ILogger logger = null
        ) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            if (!IsCompatible(adapter)) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Error_AdapterIsNotCompatibleWithHelperClass, adapter.Descriptor.Name, nameof(PollingSnapshotTagValuePush)), nameof(adapter));
            }

            return new PollingSnapshotTagValuePush(
                adapter.Descriptor.Id,
                adapter.Features.Get<ITagInfo>(),
                adapter.Features.Get<IReadSnapshotTagValues>(),
                pollingInterval,
                scheduler,
                logger
            );
        }


        /// <summary>
        /// Tests if an adapter is compatible with <see cref="PollingSnapshotTagValuePush"/>. An 
        /// adapter is compatible if it implements both <see cref="ITagInfo"/> and 
        /// <see cref="IReadSnapshotTagValues"/> features.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <paramref name="adapter"/> is compatible with 
        ///   <see cref="PollingSnapshotTagValuePush"/>, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsCompatible(IAdapter adapter) {
            if (adapter == null) {
                return false;
            }

            return 
                adapter.Features.Get<ITagInfo>() != null &&
                adapter.Features.Get<IReadSnapshotTagValues>() != null;
        }


        /// <inheritdoc/>
        protected override void OnTagAddedToSubscription(TagIdentifier tag) {
            lock (_subscribedTags) {
                _subscribedTags.Add(tag);
            }

            base.OnTagAddedToSubscription(tag);

            // Immediately get the current value.
            Scheduler.QueueBackgroundWorkItem(ct => RefreshValues(new[] { tag.Id }, ct), DisposedToken);
        }


        /// <inheritdoc/>
        protected override void OnTagRemovedFromSubscription(TagIdentifier tag) {
            lock (_subscribedTags) {
                var toBeRemoved = _subscribedTags.FindIndex(x => TagIdentifierComparer.Id.Equals(x, tag));
                if (toBeRemoved >= 0) {
                    _subscribedTags.RemoveAt(toBeRemoved);
                }
            }

            base.OnTagRemovedFromSubscription(tag);
        }


        /// <summary>
        /// Runs the dedicated polling task.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The system shutdown token.
        /// </param>
        /// <returns>
        ///   A task that will run the polling loop.
        /// </returns>
        private async Task RunSnapshotPollingLoop(CancellationToken cancellationToken) {
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    await Task.Delay(_pollingInterval, cancellationToken).ConfigureAwait(false);

                    try {
                        string[] tags;
                        lock (_subscribedTags) {
                            tags = _subscribedTags.Select(x => x.Id).ToArray();
                        }

                        await RefreshValues(tags, cancellationToken).ConfigureAwait(false);
                    }
                    catch (ChannelClosedException) {
                        // Channel was closed.
                    }
                    catch (OperationCanceledException) {
                        // Cancellation token fired.
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                        Logger.LogError(e, Resources.Log_ErrorInSnapshotSubscriptionManagerPublishLoop);
                    }
                }
            }
            catch (OperationCanceledException) {
                // Cancellation token fired.
            }
        }


        /// <summary>
        /// Gets the current values for the specified tags and publishes them.
        /// </summary>
        /// <param name="tags">
        ///   The tags.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will perform the refresh.
        /// </returns>
        private async Task RefreshValues(string[] tags, CancellationToken cancellationToken) {
            if (tags.Length == 0) {
                return;
            }

            var channel = _readSnapshotFeature.ReadSnapshotTagValues(null, new ReadSnapshotTagValuesRequest() {
                Tags = tags
            }, cancellationToken);

            while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false) && channel.TryRead(out var val) && val != null) {
                await ValueReceived(val, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            lock (_subscribedTags) {
                _subscribedTags.Clear();
            }
        }

    }
}
