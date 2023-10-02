using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Subscriptions;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {
    public class PollingSnapshotSubscriptionManager : SnapshotSubscriptionManager {

        private readonly IReadSnapshotTagValues _readSnapshotFeature;

        private readonly TimeSpan _pollingInterval;

        private readonly HashSet<string> _subscribedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        public PollingSnapshotSubscriptionManager(
            IReadSnapshotTagValues readSnapshotFeature,
            PollingSnapshotSubscriptionManagerOptions? options, 
            IBackgroundTaskService? backgroundTaskService, 
            ILogger? logger = null
        ) : base(options, backgroundTaskService, logger) {
            _readSnapshotFeature = readSnapshotFeature ?? throw new ArgumentNullException(nameof(readSnapshotFeature)); 

            if (options == null || options.PollingInterval <= TimeSpan.Zero) {
                _pollingInterval = TimeSpan.FromSeconds(1);
            }
            else {
                _pollingInterval = options.PollingInterval;
            }

            BackgroundTaskService.QueueBackgroundWorkItem(RunPollingLoopAsync, LifetimeToken);
        }


        protected override async ValueTask OnFirstSubscriberAddedAsync(IEnumerable<SubscriptionTopic> topics, CancellationToken cancellationToken) {
            await base.OnFirstSubscriberAddedAsync(topics, cancellationToken).ConfigureAwait(false);
            lock (_subscribedTags) {
                foreach (var item in topics) {
                    if (item.TopicContainsWildcard) {
                        continue;
                    }
                    _subscribedTags.Add(item.Topic);
                }
            }

            var nonWildcardTopics = topics.Where(x => !x.TopicContainsWildcard).Select(x => x.Topic).ToArray();
            if (nonWildcardTopics.Length > 0) {
                await RefreshValuesAsync(nonWildcardTopics, cancellationToken).ConfigureAwait(false);
            }
        }


        protected override async ValueTask OnLastSubscriberRemovedAsync(IEnumerable<SubscriptionTopic> topics, CancellationToken cancellationToken) {
            await base.OnLastSubscriberRemovedAsync(topics, cancellationToken).ConfigureAwait(false);
            lock (_subscribedTags) {
                foreach (var item in topics) {
                    if (item.TopicContainsWildcard) {
                        continue;
                    }
                    _subscribedTags.Remove(item.Topic);
                }
            }
        }


        private async Task RunPollingLoopAsync(CancellationToken cancellationToken) {
            do { 
                await Task.Delay(_pollingInterval, cancellationToken).ConfigureAwait(false);

                string[] tagsToPoll;
                lock (_subscribedTags) {
                    tagsToPoll = _subscribedTags.ToArray();
                }

                if (tagsToPoll.Length > 0) {
                    await RefreshValuesAsync(tagsToPoll, cancellationToken).ConfigureAwait(false);
                }
            } while (!cancellationToken.IsCancellationRequested);
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
        private async Task RefreshValuesAsync(string[] tags, CancellationToken cancellationToken) {
            if (tags.Length == 0) {
                return;
            }

            const int MaxTagsPerRequest = 100;
            var page = 0;
            bool @continue;

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

                    await PublishAsync(val, cancellationToken).ConfigureAwait(false);
                }
            } while (@continue);
        }

    }
}
