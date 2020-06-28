using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;

using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="ISnapshotTagValuePush"/> implementation.
    /// </summary>
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush, IAsyncDisposable {

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePushImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public SnapshotTagValuePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CreateSnapshotTagValueSubscriptionRequest request) {
            var result = new Subscription(context, request, this);
            await result.Start().ConfigureAwait(false);

            return result;
        }


        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            try {
                // Ensure that we delete all subscriptions for this connection.
                var request = new DeleteSnapshotSubscriptionRequest() {
                    SubscriptionId = string.Empty
                };
                var response = CreateClient<TagValuesService.TagValuesServiceClient>().DeleteSnapshotSubscriptionAsync(request, GetCallOptions(null, default));
                await response.ResponseAsync.ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch { }
#pragma warning restore CA1031 // Do not catch general exception types
        }


        /// <summary>
        /// <see cref="SnapshotTagValueSubscriptionBase"/> implementation that receives data via a
        /// gRPC channel.
        /// </summary>
        private class Subscription : SnapshotTagValueSubscriptionBase {

            /// <summary>
            /// The creating feature.
            /// </summary>
            private readonly SnapshotTagValuePushImpl _feature;

            /// <summary>
            /// The subscription creation options.
            /// </summary>
            private readonly CreateSnapshotTagValueSubscriptionRequest _request;

            /// <summary>
            /// The remote subscription ID.
            /// </summary>
            private string _subscriptionId;

            /// <summary>
            /// The client for the gRPC service.
            /// </summary>
            private readonly TagValuesService.TagValuesServiceClient _client;

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
            ///   The subscription creation request.
            /// </param>
            /// <param name="feature">
            ///   The push feature.
            /// </param>
            internal Subscription(
                IAdapterCallContext context,
                CreateSnapshotTagValueSubscriptionRequest request,
                SnapshotTagValuePushImpl feature
            ) : base(context, feature.AdapterId) {
                _feature = feature;
                _request = request ?? new CreateSnapshotTagValueSubscriptionRequest();
                _client = _feature.CreateClient<TagValuesService.TagValuesServiceClient>();
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
                if (string.IsNullOrWhiteSpace(_subscriptionId)) {
                    tcs.TrySetResult(false);
                    return;
                }
                
                GrpcCore.AsyncServerStreamingCall<TagValueQueryResult> grpcChannel;

                try {
                    grpcChannel = _client.CreateSnapshotPushChannel(new CreateSnapshotPushChannelRequest() {
                        SubscriptionId = _subscriptionId,
                        Tag = tagId
                    }, _feature.GetCallOptions(Context, cancellationToken));
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

                while (await grpcChannel.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                    if (grpcChannel.ResponseStream.Current == null) {
                        continue;
                    }

                    await ValueReceived(
                        grpcChannel.ResponseStream.Current.ToAdapterTagValueQueryResult(),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
            }


            /// <inheritdoc/>
            protected override async Task Init(CancellationToken cancellationToken) {
                await base.Init(cancellationToken).ConfigureAwait(false);

                // Create the subscription.
                var request = new CreateSnapshotSubscriptionRequest() {
                    AdapterId = _feature.AdapterId
                };
                if (_request.Properties != null) {
                    foreach (var item in _request.Properties) {
                        request.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }

                var response = _client.CreateSnapshotSubscriptionAsync(
                    request,
                    _feature.GetCallOptions(Context, cancellationToken)
                );

                var result = await response.ResponseAsync.ConfigureAwait(false);
                _subscriptionId = result.SubscriptionId;
            }


            /// <inheritdoc/>
            protected override ValueTask<Adapter.RealTimeData.TagIdentifier> ResolveTag(IAdapterCallContext context, string tag, CancellationToken cancellationToken) {
                return new ValueTask<Adapter.RealTimeData.TagIdentifier>(new Adapter.RealTimeData.TagIdentifier(tag, tag));
            }


            /// <inheritdoc/>
            protected override Task OnTagAdded(Adapter.RealTimeData.TagIdentifier tag) {
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
            protected override Task OnTagRemoved(Adapter.RealTimeData.TagIdentifier tag) {
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
                    _feature.TaskScheduler.QueueBackgroundWorkItem(async ct => {
                        var response = _client.DeleteSnapshotSubscriptionAsync(new DeleteSnapshotSubscriptionRequest() { 
                            SubscriptionId = _subscriptionId
                        }, _feature.GetCallOptions(Context, ct));

                        await response.ResponseAsync.ConfigureAwait(false);
                    });
                }
            }

        }

    }
}
