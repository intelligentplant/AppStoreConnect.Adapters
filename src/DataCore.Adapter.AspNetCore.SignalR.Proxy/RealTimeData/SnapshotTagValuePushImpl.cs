using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter;
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
        public async Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context) {
            var result = new Subscription(context, AdapterId, this);
            await result.Start().ConfigureAwait(false);

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
            /// The feature.
            /// </summary>
            private readonly SnapshotTagValuePushImpl _push;


            /// <summary>
            /// Creates a new <see cref="Subscription"/>.
            /// </summary>
            /// <param name="context">
            ///   The adapter call context for the subscription owner.
            /// </param>
            /// <param name="adapterId">
            ///   The adapter ID.
            /// </param>
            /// <param name="push">
            ///   The push feature.
            /// </param>
            internal Subscription(
                IAdapterCallContext context, 
                string adapterId,
                SnapshotTagValuePushImpl push
            ) : base (context, adapterId) {
                _adapterId = adapterId;
                _push = push;
            }


            /// <inheritdoc/>
            protected override async Task RunSubscription(ChannelReader<SnapshotTagValueSubscriptionChange> channel, CancellationToken cancellationToken) {
                var subChangesChannel = Channel.CreateUnbounded<UpdateSnapshotTagValueSubscriptionRequest>();
                
                var hubChannel = await _push.GetClient().TagValues.CreateSnapshotTagValueChannelAsync(
                    _adapterId,
                    subChangesChannel,
                    cancellationToken
                ).ConfigureAwait(false);

                channel.RunBackgroundOperation(async (ch, ct) => { 
                    try {
                        while (!ct.IsCancellationRequested) {
                            var change = await ch.ReadAsync(ct).ConfigureAwait(false);
                            try {
                                await subChangesChannel.Writer.WriteAsync(change.Request, ct).ConfigureAwait(false);
                                change.SetResult(true);
                            }
                            catch {
                                change.SetResult(false);
                                throw;
                            }
                        }
                    }
                    catch (ChannelClosedException) { }
                    catch (OperationCanceledException) { }
                }, _push.TaskScheduler, cancellationToken);

                // Wait for and discard the initial "subscription created" placeholder.
                await hubChannel.ReadAsync(cancellationToken).ConfigureAwait(false);

                await hubChannel.ForEachAsync(async val => {
                    if (val == null) {
                        return;
                    }
                    await ValueReceived(val, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            protected override ValueTask<TagIdentifier> ResolveTag(IAdapterCallContext context, string tag, CancellationToken cancellationToken) {
                return new ValueTask<TagIdentifier>(new TagIdentifier(tag, tag));
            }


            /// <inheritdoc/>
            protected override Task OnTagAdded(TagIdentifier tag) {
                return Task.CompletedTask;
            }


            /// <inheritdoc/>
            protected override Task OnTagRemoved(TagIdentifier tag) {
                return Task.CompletedTask;
            }

        }
        
    }
}
