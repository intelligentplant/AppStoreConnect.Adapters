using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        public SnapshotTagValuePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public ISnapshotTagValueSubscription Subscribe(IAdapterCallContext context) {
            var result = new Subscription(context, this);
            result.Start();

            return result;
        }


        /// <summary>
        /// <see cref="SnapshotTagValueSubscription"/> implementation that sends/receives data via 
        /// a SignalR channel.
        /// </summary>
        private class Subscription : SnapshotTagValueSubscription {

            private readonly SnapshotTagValuePushImpl _push;

            /// <summary>
            /// Creates a new <see cref="Subscription"/>.
            /// </summary>
            /// <param name="context">
            ///   The adapter call context for the subscription owner.
            /// </param>
            /// <param name="push">
            ///   The push feature.
            /// </param>
            internal Subscription(
                IAdapterCallContext context,
                SnapshotTagValuePushImpl push
            ) : base(context) {
                _push = push;
            }


            protected override async Task ProcessTagsChannel(ChannelReader<UpdateSnapshotTagValueSubscriptionRequest> channel, CancellationToken cancellationToken) {
                var client = _push.CreateClient<TagValuesService.TagValuesServiceClient>();
                var duplexCall = client.CreateSnapshotPushChannel(_push.GetCallOptions(Context, cancellationToken));

                // Run another background task to read subscription changes and pass them to the 
                // remote service.
                channel.RunBackgroundOperation(async (ch, ct) => {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!ch.TryRead(out var change) || change == null) {
                            continue;
                        }

                        await duplexCall.RequestStream.WriteAsync(
                            new CreateSnapshotPushChannelRequest() {
                                AdapterId = _push.AdapterId,
                                Tag = change.Tag ?? string.Empty,
                                Action = change.Action == Common.SubscriptionUpdateAction.Subscribe
                                    ? SubscriptionUpdateAction.Subscribe
                                    : SubscriptionUpdateAction.Unsubscribe
                            }
                        ).ConfigureAwait(false);
                    }
                }, _push.TaskScheduler, cancellationToken);

                // Read value changes.
                while (await duplexCall.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (duplexCall.ResponseStream.Current == null) {
                        continue;
                    }

                    await ValueReceived(
                        duplexCall.ResponseStream.Current.ToAdapterTagValueQueryResult(), 
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }
        }

    }
}
