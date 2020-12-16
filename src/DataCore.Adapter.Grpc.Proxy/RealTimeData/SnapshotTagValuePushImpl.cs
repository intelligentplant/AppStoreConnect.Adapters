using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

using GrpcCore = Grpc.Core;

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
        public async Task<ChannelReader<Adapter.RealTimeData.TagValueQueryResult>> Subscribe(
            IAdapterCallContext context, 
            CreateSnapshotTagValueSubscriptionRequest request, 
            ChannelReader<TagValueSubscriptionUpdate> channel,
            CancellationToken cancellationToken
        ) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            GrpcAdapterProxy.ValidateObject(request);

            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var client = CreateClient<TagValuesService.TagValuesServiceClient>();
            var grpcStream = client.CreateSnapshotPushChannel(GetCallOptions(context, cancellationToken));

            var createSubscriptionMessage = new CreateSnapshotPushChannelMessage() {
                AdapterId = AdapterId,
                PublishInterval = request.PublishInterval > TimeSpan.Zero
                    ? Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(request.PublishInterval)
                    : Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(TimeSpan.Zero)
            };
            createSubscriptionMessage.Tags.Add(request.Tags?.Where(x => x != null) ?? Array.Empty<string>());
            if (request.Properties != null) {
                foreach (var item in request.Properties) {
                    createSubscriptionMessage.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            // Create the subscription.
            await grpcStream.RequestStream.WriteAsync(new CreateSnapshotPushChannelRequest() { 
                Create = createSubscriptionMessage
            }).ConfigureAwait(false);

            // Stream subscription changes.
            channel.RunBackgroundOperation(async (ch, ct) => { 
                while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    while (ch.TryRead(out var update)) {
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
                }
            }, BackgroundTaskService, cancellationToken);

            // Stream the results.
            var result = ChannelExtensions.CreateTagValueChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                // Read tag values.
                while (await grpcStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcStream.ResponseStream.Current == null) {
                        continue;
                    }

                    await result.Writer.WriteAsync(grpcStream.ResponseStream.Current.ToAdapterTagValueQueryResult(), ct).ConfigureAwait(false);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }
}
