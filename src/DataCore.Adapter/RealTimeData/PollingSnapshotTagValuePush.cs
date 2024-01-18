using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

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
        /// <param name="readSnapshotFeature">
        ///   An <see cref="IReadSnapshotTagValues"/> adapter feature that will be used to poll for 
        ///   new values on a periodic basis.
        /// </param>
        /// <param name="options">
        ///   The feature options.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running background tasks. If the 
        ///   value specified is <see langword="null"/>, <see cref="BackgroundTaskService.Default"/> 
        ///   will be used.
        /// </param>
        /// <param name="logger">
        ///   The logger to use. Can be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="readSnapshotFeature"/> is <see langword="null"/>.
        /// </exception>
        public PollingSnapshotTagValuePush(
            IReadSnapshotTagValues readSnapshotFeature,
            PollingSnapshotTagValuePushOptions? options,
            IBackgroundTaskService? backgroundTaskService,
            ILogger? logger
        ) : base(
            options, 
            backgroundTaskService,
            logger
        ) {
            _readSnapshotFeature = readSnapshotFeature ?? throw new ArgumentNullException(nameof(readSnapshotFeature));
            _pollingInterval = options?.PollingInterval > TimeSpan.Zero
                ? options.PollingInterval
                : DefaultPollingInterval;

            BackgroundTaskService.QueueBackgroundWorkItem(RunSnapshotPollingLoop, DisposedToken);
        }


        /// <inheritdoc/>
        protected override async Task OnTagsAdded(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            lock (_subscribedTags) {
                _subscribedTags.AddRange(tags);
            }

            await base.OnTagsAdded(tags, cancellationToken).ConfigureAwait(false);

            // Immediately get the current values.
            await RefreshValues(tags.Select(x => x.Id).ToArray(), cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override Task OnTagsRemoved(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            lock (_subscribedTags) {
                foreach (var tag in tags) {
                    var toBeRemoved = _subscribedTags.FindIndex(x => TagIdentifierComparer.Id.Equals(x, tag));
                    if (toBeRemoved >= 0) {
                        _subscribedTags.RemoveAt(toBeRemoved);
                    }
                }
            }

            return base.OnTagsRemoved(tags, cancellationToken);
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
                    catch (Exception e) {
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

            const int MaxTagsPerRequest = 100;
            var page = 0;
            var @continue = false;

            do {
                ++page;
                var pageTags = tags.Skip((page - 1) * MaxTagsPerRequest).Take(MaxTagsPerRequest).ToArray();
                if (pageTags.Length == 0) {
                    break;
                }
                @continue = pageTags.Length == MaxTagsPerRequest;

                await foreach (var val in _readSnapshotFeature.ReadSnapshotTagValues(
                    new DefaultAdapterCallContext(),
                    new ReadSnapshotTagValuesRequest() {
                        Tags = tags
                    }, cancellationToken
                ).ConfigureAwait(false)) {
                    if (val == null) {
                        continue;
                    }
                    await ValueReceived(val, cancellationToken).ConfigureAwait(false);
                }
            } while (@continue);
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            lock (_subscribedTags) {
                _subscribedTags.Clear();
            }
        }

    }


    /// <summary>
    /// Options for <see cref="PollingSnapshotTagValuePush"/>.
    /// </summary>
    public class PollingSnapshotTagValuePushOptions : SnapshotTagValuePushOptions {

        /// <summary>
        /// The polling interval to use when refreshing values for subscribed tags.
        /// </summary>
        /// <remarks>
        ///   If the value specified is less than or equal to <see cref="TimeSpan.Zero"/>, 
        ///   <see cref="PollingSnapshotTagValuePush.DefaultPollingInterval"/> will be used.
        /// </remarks>
        public TimeSpan PollingInterval { get; set; }

    }
}
