using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;

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
            var result = new Subscription(context, this);
            await result.Start().ConfigureAwait(false);

            return result;
        }


        /// <summary>
        /// <see cref="SnapshotTagValueSubscriptionBase"/> implementation that sends/receives data via 
        /// a SignalR channel.
        /// </summary>
        private class Subscription : SnapshotTagValueSubscriptionBase {

            /// <summary>
            /// The feature.
            /// </summary>
            private readonly SnapshotTagValuePushImpl _push;

            /// <summary>
            /// Holds the lifetime cancellation token for each subscribed tag.
            /// </summary>
            private readonly ConcurrentDictionary<string, CancellationTokenSource> _tagSubscriptionLifetimes = new ConcurrentDictionary<string, CancellationTokenSource>();


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
            ) : base (context, push.AdapterId) {
                _push = push;
            }


            
            /// <summary>
            /// Creates an processes a subscription to the specified tag ID.
            /// </summary>
            /// <param name="tagId">
            ///   The tag ID.
            /// </param>
            /// <param name="tcs">
            ///   A <see cref="TaskCompletionSource{TResult}"/> that will be completed once the 
            ///   tag subscription has been created.
            /// </param>
            /// <param name="cancellationToken">
            ///   The cancellation token for the operation.
            /// </param>
            /// <returns>
            ///   A long-running task that will run the subscription until the cancellation token 
            ///   fires.
            /// </returns>
            private async Task RunTagSubscription(string tagId, TaskCompletionSource<bool> tcs, CancellationToken cancellationToken) {
                ChannelReader<TagValueQueryResult> hubChannel;

                try {
                    hubChannel = await _push.GetClient().TagValues.CreateSnapshotTagValueChannelAsync(
                        _push.AdapterId,
                        tagId,
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    tcs.TrySetCanceled(cancellationToken);
                    throw;
                } 
                catch (Exception e) {
                    tcs.TrySetException(e);
                    throw;
                }
                finally {
                    tcs.TrySetResult(true);
                }

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
                if (CancellationToken.IsCancellationRequested) {
                    return Task.CompletedTask;
                }

                var added = false;
                var ctSource = _tagSubscriptionLifetimes.GetOrAdd(tag.Id, k => {
                    added = true;
                    return new CancellationTokenSource();
                });

                if (added) {
                    var tcs = new TaskCompletionSource<bool>();
                    _push.TaskScheduler.QueueBackgroundWorkItem(ct => RunTagSubscription(tag.Id, tcs, ct), ctSource.Token, CancellationToken);
                    return tcs.Task;
                }

                return Task.CompletedTask;
            }


            /// <inheritdoc/>
            protected override Task OnTagRemoved(TagIdentifier tag) {
                if (CancellationToken.IsCancellationRequested) {
                    return Task.CompletedTask;
                }

                if (_tagSubscriptionLifetimes.TryRemove(tag.Id, out var ctSource)) {
                    ctSource.Cancel();
                    ctSource.Dispose();
                }

                return Task.CompletedTask;
            }


            /// <inheritdoc/>
            protected override void OnCancelled() {
                base.OnCancelled();

                foreach (var item in _tagSubscriptionLifetimes.Values.ToArray()) {
                    item.Cancel();
                    item.Dispose();
                }

                _tagSubscriptionLifetimes.Clear();
            }

        }
        
    }
}
