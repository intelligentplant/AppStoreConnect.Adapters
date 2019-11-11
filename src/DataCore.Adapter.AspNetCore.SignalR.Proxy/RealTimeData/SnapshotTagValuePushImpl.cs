using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;
using Microsoft.Extensions.Logging;

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
        public async Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CancellationToken cancellationToken) {
            ISnapshotTagValueSubscription result = new SnapshotTagValueSubscription(this);

            try {
                await result.StartAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch {
                await result.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            return result;
        }

        /// <summary>
        /// <see cref="ISnapshotTagValueSubscription"/> implementation for the 
        /// <see cref="ISnapshotTagValuePush"/> feature.
        /// </summary>
        private class SnapshotTagValueSubscription : Adapter.RealTimeData.SnapshotTagValueSubscription {

            /// <summary>
            /// The feature instance.
            /// </summary>
            private readonly SnapshotTagValuePushImpl _feature;

            /// <summary>
            /// The underlying adapter SignalR client.
            /// </summary>
            private readonly AdapterSignalRClient _client;

            /// <summary>
            /// The tags that have been added to the subscription.
            /// </summary>
            private readonly HashSet<string> _tags = new HashSet<string>();

            /// <inheritdoc />
            public override int Count {
                get { return _tags.Count; }
            }


            /// <summary>
            /// Creates a new <see cref="SnapshotTagValueSubscription"/> object.
            /// </summary>
            /// <param name="feature">
            ///   The feature instance.
            /// </param>
            internal SnapshotTagValueSubscription(SnapshotTagValuePushImpl feature) {
                _feature = feature;
                _client = _feature.GetClient();
                _client.Reconnected += OnClientReconnected;
            }


            /// <summary>
            /// Handles SignalR client reconnections.
            /// </summary>
            /// <param name="connectionId">
            ///   The updated connection ID.
            /// </param>
            /// <returns>
            ///   A task that will re-create the subscription to the remote adapter.
            /// </returns>
            private async Task OnClientReconnected(string connectionId) {
                await CreateSignalRChannel().ConfigureAwait(false);
            }


            /// <summary>
            /// Creates a SignalR subscription for tag values from the remote adapter and then 
            /// starts a background task to forward received values to this subscription's channel.
            /// </summary>
            /// <returns>
            ///   A task that will complete as soon as the subscription has been established. 
            ///   Forwarding of received values will continue in a background task.
            /// </returns>
            private async Task CreateSignalRChannel() {
                // If this is a reconnection, we might have remote tags that we need to 
                // resubscribe to.
                string[] tags;

                lock (_tags) {
                    tags = _tags.ToArray();
                }

                var hubChannel = await _client.TagValues.CreateSnapshotTagValueChannelAsync(
                    _feature.AdapterId,
                    tags,
                    SubscriptionCancelled
                ).ConfigureAwait(false);

                _feature.TaskScheduler.QueueBackgroundWorkItem(async ct => {
                    try {
                        await hubChannel.Forward(Writer, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException e) {
                        // Subscription was cancelled.
                        Writer.TryComplete(e);
                    }
                    catch (Exception e) {
                        // Another error (e.g. SignalR disconnection) occurred. In this situation, 
                        // we won't complete the Writer in case we manage to reconnect.
                        _feature.Logger.LogError(e, Resources.Log_SnapshotTagValueSubscriptionError);
                    }
                }, null, SubscriptionCancelled);
            }


            /// <inheritdoc />
            protected override async ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
                await CreateSignalRChannel().WithCancellation(cancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc />
            public override ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken) {
                var result = ChannelExtensions.CreateTagIdentifierChannel(-1);

                result.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var hubChannel = await _client.TagValues.GetSnapshotTagValueChannelSubscriptionsAsync(
                        _feature.AdapterId,
                        ct
                    ).ConfigureAwait(false);

                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
                }, true, _feature.TaskScheduler, cancellationToken);

                return result;
            }


            /// <inheritdoc />
            public override async Task<int> AddTagsToSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                var tagsToAdd = new List<string>();
                int count;

                lock (_tags) {
                    count = _tags.Count;

                    foreach (var tag in tagNamesOrIds) {
                        if (_tags.Add(tag)) {
                            tagsToAdd.Add(tag);
                        }
                    }
                }

                if (tagsToAdd.Count == 0) {
                    return count;
                }

                return await _client.TagValues.AddTagsToSnapshotTagValueChannelAsync(
                    _feature.AdapterId,
                    tagsToAdd,
                    cancellationToken
                ).ConfigureAwait(false);
            }


            /// <inheritdoc />
            public override async Task<int> RemoveTagsFromSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
                var tagsToRemove = new List<string>();
                int count;

                lock (_tags) {
                    count = _tags.Count;

                    foreach (var tag in tagNamesOrIds) {
                        if (_tags.Remove(tag)) {
                            tagsToRemove.Add(tag);
                        }
                    }
                }

                if (tagsToRemove.Count == 0) {
                    return count;
                }

                return await _client.TagValues.RemoveTagsFromSnapshotTagValueChannelAsync(
                    _feature.AdapterId,
                    tagsToRemove,
                    cancellationToken
                ).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    _client.Reconnected -= OnClientReconnected;
                    lock (_tags) {
                        _tags.Clear();
                    }
                }
            }


            /// <inheritdoc/>
            protected override ValueTask DisposeAsync(bool disposing) {
                Dispose(disposing);
                return default;
            }

        }
    }
}
