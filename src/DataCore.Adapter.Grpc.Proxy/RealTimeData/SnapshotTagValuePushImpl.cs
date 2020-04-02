using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        public SnapshotTagValuePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context) {
            var result = new Subscription(context, this);
            await result.Start().ConfigureAwait(false);

            return result;
        }


        /// <summary>
        /// <see cref="SnapshotTagValueSubscriptionBase"/> implementation that sends/receives data via 
        /// a SignalR channel.
        /// </summary>
        private class Subscription : SnapshotTagValueSubscriptionBase {

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
            ) : base(context, push.AdapterId) {
                _push = push;
            }


            /// <inheritdoc/>
            protected override async Task RunSubscription(ChannelReader<SnapshotTagValueSubscriptionChange> channel, CancellationToken cancellationToken) {
                var client = _push.CreateClient<TagValuesService.TagValuesServiceClient>();
                var duplexCall = client.CreateSnapshotPushChannel(_push.GetCallOptions(Context, cancellationToken));

                // Run another background task to read subscription changes and pass them to the 
                // remote service.
                channel.RunBackgroundOperation(async (ch, ct) => {
                    while (await ch.WaitToReadAsync(ct).ConfigureAwait(false)) {
                        if (!ch.TryRead(out var change) || change == null) {
                            continue;
                        }

                        try {
                            await duplexCall.RequestStream.WriteAsync(
                                new CreateSnapshotPushChannelRequest() {
                                    AdapterId = _push.AdapterId,
                                    Tag = change.Request.Tag ?? string.Empty,
                                    Action = change.Request.Action == Common.SubscriptionUpdateAction.Subscribe
                                        ? SubscriptionUpdateAction.Subscribe
                                        : SubscriptionUpdateAction.Unsubscribe
                                }
                            ).ConfigureAwait(false);
                            change.SetResult(true);
                        }
                        catch {
                            change.SetResult(false);
                            throw;
                        }
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


            /// <inheritdoc/>
            protected override ValueTask<Adapter.RealTimeData.TagIdentifier> ResolveTag(IAdapterCallContext context, string tag, CancellationToken cancellationToken) {
                return new ValueTask<Adapter.RealTimeData.TagIdentifier>(new Adapter.RealTimeData.TagIdentifier(tag, tag));
            }


            /// <inheritdoc/>
            protected override Task OnTagAdded(Adapter.RealTimeData.TagIdentifier tag) {
                return Task.CompletedTask;
            }


            /// <inheritdoc/>
            protected override Task OnTagRemoved(Adapter.RealTimeData.TagIdentifier tag) {
                return Task.CompletedTask;
            }

        }

    }
}
