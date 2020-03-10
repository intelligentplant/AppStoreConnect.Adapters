using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="ISnapshotTagValuePush"/>.
    /// </summary>
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePushImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public SnapshotTagValuePushImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public ISnapshotTagValueSubscription Subscribe(IAdapterCallContext context) {
            var result = new Subscription(context, AdapterId, GetClient());
            result.Start();

            return result;
        }


        /// <summary>
        /// <see cref="SnapshotTagValueSubscriptionBase"/> implementation that sends/receives data via 
        /// a SignalR channel.
        /// </summary>
        private class Subscription : SnapshotTagValueSubscriptionBase {

            /// <summary>
            /// The adapter ID.
            /// </summary>
            private readonly string _adapterId;

            /// <summary>
            /// The SignalR client.
            /// </summary>
            private readonly Client.AdapterSignalRClient _client;


            /// <summary>
            /// Creates a new <see cref="Subscription"/>.
            /// </summary>
            /// <param name="context">
            ///   The adapter call context for the subscription owner.
            /// </param>
            /// <param name="adapterId">
            ///   The adapter ID.
            /// </param>
            /// <param name="client">
            ///   The SignalR client.
            /// </param>
            internal Subscription(
                IAdapterCallContext context, 
                string adapterId,
                Client.AdapterSignalRClient client
            ) : base (context) {
                _adapterId = adapterId;
                _client = client;
            }


            protected override async Task ProcessSubscriptionChangesChannel(ChannelReader<UpdateSnapshotTagValueSubscriptionRequest> channel, CancellationToken cancellationToken) {
                var hubChannel = await _client.TagValues.CreateSnapshotTagValueChannelAsync(
                    _adapterId,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false);

                await hubChannel.ForEachAsync(async val => {
                    await ValueReceived(val, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
        }
        
    }
}
