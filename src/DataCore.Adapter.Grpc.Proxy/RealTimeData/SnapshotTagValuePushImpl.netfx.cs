#if NETFRAMEWORK

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;


namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    partial class SnapshotTagValuePushImpl {

        /// <summary>
        /// All active streaming calls, indexed by subscription ID and then tag ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IDisposable>> _activeStreamingCalls = new ConcurrentDictionary<string, ConcurrentDictionary<string, IDisposable>>(StringComparer.Ordinal);


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePush"/> instance that will handle all 
        /// individual tag subscriptions for a single call to <see cref="Subscribe"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="client">
        ///   The gRPC client to use.
        /// </param>
        /// <param name="request">
        ///   The initial <see cref="CreateSnapshotPushChannelRequest"/>.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the subscription.
        /// </param>
        /// <returns>
        ///   A new <see cref="SnapshotTagValuePush"/> instance.
        /// </returns>
        private SnapshotTagValuePush CreateInnerHandler(
            IAdapterCallContext context,
            TagValuesService.TagValuesServiceClient client,
            CreateSnapshotTagValueSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            var options = new SnapshotTagValuePushOptions() { 
                OnTagSubscriptionsAdded = async (instance, tags, _) => await OnTagsAddedAsync((SnapshotTagValuePush) instance, context, client, tags, request, cancellationToken).ConfigureAwait(false),
                OnTagSubscriptionsRemoved = async (instance, tags, _) => await OnTagsRemovedAsync((SnapshotTagValuePush) instance, tags).ConfigureAwait(false),
                IsTopicMatch = (subscribed, actual, ct) => new ValueTask<bool>(subscribed.Id.Equals(actual.Id, StringComparison.OrdinalIgnoreCase) || subscribed.Name.Equals(actual.Name, StringComparison.OrdinalIgnoreCase))
            };
            var result = new SnapshotTagValuePush(options, BackgroundTaskService, Logger);

            return result;
        }


        /// <summary>
        /// Called when tags are added to the subscription.
        /// </summary>
        private Task OnTagsAddedAsync(
            SnapshotTagValuePush instance,
            IAdapterCallContext context,
            TagValuesService.TagValuesServiceClient client,
            IEnumerable<Adapter.Tags.TagIdentifier> tags,
            CreateSnapshotTagValueSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            foreach (var tag in tags) {
                cancellationToken.ThrowIfCancellationRequested();

                var grpcRequest = new CreateSnapshotPushChannelMessage() {
                    AdapterId = AdapterId
                };

                grpcRequest.Tags.Add(tag.Id);
                
                if (request.PublishInterval > TimeSpan.Zero) {
                    grpcRequest.PublishInterval = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(request.PublishInterval);
                }
                
                if (request.Properties != null) {
                    foreach (var item in request.Properties) {
                        grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }
                
                var subscription = client.CreateFixedSnapshotPushChannel(grpcRequest, GetCallOptions(context, cancellationToken));

                var activeCallsForSubscriber = _activeStreamingCalls.GetOrAdd(instance.Id, _ => new ConcurrentDictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase));
                activeCallsForSubscriber[tag.Id] = subscription;

                BackgroundTaskService.QueueBackgroundWorkItem(async ct => { 
                    while (await subscription.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (subscription.ResponseStream.Current == null) {
                            continue;
                        }
                        await instance.ValueReceived(subscription.ResponseStream.Current.ToAdapterTagValueQueryResult(), ct).ConfigureAwait(false);
                    }
                }, cancellationToken);
            }

            return Task.CompletedTask;
        }


        /// <summary>
        /// Called when tags are removed from the subscription.
        /// </summary>
        private Task OnTagsRemovedAsync(
            SnapshotTagValuePush instance,
            IEnumerable<Adapter.Tags.TagIdentifier> tags
        ) {
            if (!_activeStreamingCalls.TryGetValue(instance.Id, out var activeCallsForSubscriber)) {
                return Task.CompletedTask;
            }

            foreach (var tag in tags) {
                if (activeCallsForSubscriber.TryRemove(tag.Id, out var subscription)) {
                    subscription.Dispose();
                }
            }

            return Task.CompletedTask;
        }



        /// <inheritdoc />
        public async IAsyncEnumerable<Adapter.RealTimeData.TagValueQueryResult> Subscribe(
            IAdapterCallContext context,
            CreateSnapshotTagValueSubscriptionRequest request,
            IAsyncEnumerable<TagValueSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            
            using var handler = CreateInnerHandler(context, client, request, cancellationToken);

            try {
                await foreach (var item in handler.Subscribe(context, request, channel, cancellationToken).ConfigureAwait(false)) {
                    yield return item;
                }
            }
            finally {
                if (_activeStreamingCalls.TryRemove(handler.Id, out var activeCallsForSubscriber)) {
                    foreach (var item in activeCallsForSubscriber.Values) {
                        item.Dispose();
                    }
                }
            }
        }

    }
}

#endif
