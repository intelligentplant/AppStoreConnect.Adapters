using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="ISnapshotTagValuePush"/> implementation.
    /// </summary>
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePushImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public SnapshotTagValuePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<Adapter.RealTimeData.TagValueQueryResult> Subscribe(
            IAdapterCallContext context, 
            CreateSnapshotTagValueSubscriptionRequest request, 
            IAsyncEnumerable<TagValueSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();

            var createSubscriptionMessage = new CreateSnapshotPushChannelMessage() {
                AdapterId = AdapterId                
            };
            createSubscriptionMessage.Tags.Add(request.Tags?.Where(x => x != null) ?? Array.Empty<string>());
            if (request.Properties != null) {
                foreach (var item in request.Properties) {
                    createSubscriptionMessage.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcStream = client.CreateSnapshotPushChannel(GetCallOptions(context, ctSource.Token))) {

                // Create the subscription.
                await grpcStream.RequestStream.WriteAsync(new CreateSnapshotPushChannelRequest() {
                    Create = createSubscriptionMessage
                }).ConfigureAwait(false);

                // Stream subscription changes.
                Proxy.BackgroundTaskService.QueueBackgroundWorkItem(async ct => {
                    await foreach (var update in channel.WithCancellation(ct).ConfigureAwait(false)) {
                        if (update == null) {
                            continue;
                        }

                        var msg = new UpdateSnapshotPushChannelMessage() {
                            Action = update.Action == Common.SubscriptionUpdateAction.Subscribe
                                ? SubscriptionUpdateAction.Subscribe
                                : SubscriptionUpdateAction.Unsubscribe
                        };
                        msg.Tags.Add(update.Tags.Where(x => x != null));
                        if (msg.Tags.Count == 0) {
                            continue;
                        }

                        await grpcStream.RequestStream.WriteAsync(new CreateSnapshotPushChannelRequest() {
                            Update = msg
                        }).ConfigureAwait(false);
                    }
                }, ctSource.Token);

                // Stream the results.
                while (await grpcStream.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    if (grpcStream.ResponseStream.Current == null) {
                        continue;
                    }

                    yield return grpcStream.ResponseStream.Current.ToAdapterTagValueQueryResult();
                }
            }
        }

    }
}
