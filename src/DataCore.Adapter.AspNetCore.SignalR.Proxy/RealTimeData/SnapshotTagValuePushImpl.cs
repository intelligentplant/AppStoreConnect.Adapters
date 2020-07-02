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
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush, IAsyncDisposable {

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePushImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public SnapshotTagValuePushImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CreateSnapshotTagValueSubscriptionRequest request) {
            var result = new Subscription(context, request, this);
            await result.Start().ConfigureAwait(false);

            return result;
        }


        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            try {
                // Ensure that all we delete all subscriptions for the connection.
                await GetClient().TagValues.DeleteSnapshotTagValueSubscriptionAsync(string.Empty, default).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
        }


        /// <summary>
        /// <see cref="SnapshotTagValueSubscriptionBase"/> implementation that sends/receives data via 
        /// a SignalR channel.
        /// </summary>
        private class Subscription : SnapshotTagValueSubscriptionBase {

            /// <summary>
            /// The feature.
            /// </summary>
            private readonly SnapshotTagValuePushImpl _feature;

            /// <summary>
            /// The subscription request.
            /// </summary>
            private readonly CreateSnapshotTagValueSubscriptionRequest _request;

            /// <summary>
            /// The remote subscription ID.
            /// </summary>
            private string _subscriptionId;

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
            /// <param name="request">
            ///   The subscription request settings.
            /// </param>
            /// <param name="feature">
            ///   The push feature.
            /// </param>
            internal Subscription(
                IAdapterCallContext context,
                CreateSnapshotTagValueSubscriptionRequest request,
                SnapshotTagValuePushImpl feature
            ) : base (context, feature.AdapterId, TimeSpan.Zero) {
                // We specify TimeSpan.Zero in the base constructor call because we will let the 
                // remote system handle the publish interval.
                _feature = feature;
                _request = request ?? new CreateSnapshotTagValueSubscriptionRequest();
            }


            /// <inheritdoc/>
            protected override async Task Init(CancellationToken cancellationToken) {
                await base.Init(cancellationToken).ConfigureAwait(false);

                // Create the subscription.
                _subscriptionId = await _feature.GetClient().TagValues.CreateSnapshotTagValueSubscriptionAsync(
                    _feature.AdapterId, 
                    _request, 
                    cancellationToken
                ).ConfigureAwait(false);
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
                    _feature.TaskScheduler.QueueBackgroundWorkItem(ct => RunTagSubscription(tag.Id, tcs, ct), ctSource.Token, CancellationToken);
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
                if (!string.IsNullOrWhiteSpace(_subscriptionId)) {
                    // Notify server of cancellation.
                    _feature.TaskScheduler.QueueBackgroundWorkItem(ct => _feature.GetClient().TagValues.DeleteSnapshotTagValueSubscriptionAsync(_subscriptionId, ct));
                }
            }


            /// <summary>
            /// Creates and processes a subscription to the specified tag ID.
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
                    hubChannel = await _feature.GetClient().TagValues.CreateSnapshotTagValueChannelAsync(
                        _subscriptionId,
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

        }
        
    }
}
