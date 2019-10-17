using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
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
        public async Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CancellationToken cancellationToken) {
            ISnapshotTagValueSubscription result = new SnapshotTagValueSubscription(
                AdapterId,
                GetClient()
            );

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
            /// The adapter ID for the subscription.
            /// </summary>
            private readonly string _adapterId;

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
            /// <param name="adapterId">
            ///   The adapter ID.
            /// </param>
            /// <param name="client">
            ///   The adapter SignalR client.
            /// </param>
            public SnapshotTagValueSubscription(string adapterId, AdapterSignalRClient client) {
                _adapterId = adapterId;
                _client = client;
            }


            /// <inheritdoc />
            protected override async ValueTask StartAsync(IAdapterCallContext context, CancellationToken cancellationToken) {
                var tcs = new TaskCompletionSource<int>();

                Writer.RunBackgroundOperation(async (ch, ct) => {
                    string[] tags;
                    lock (_tags) {
                        tags = _tags.ToArray();
                    }

                    ChannelReader<TagValueQueryResult> hubChannel;

                    try {
                        hubChannel = await _client.TagValues.CreateSnapshotTagValueChannelAsync(
                            _adapterId,
                            tags,
                            ct
                        ).ConfigureAwait(false);

                        tcs.TrySetResult(0);
                    }
                    catch (OperationCanceledException) {
                        tcs.TrySetCanceled();
                        throw;
                    }
                    catch (Exception e) {
                        tcs.TrySetException(e);
                        throw;
                    }

                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
                }, true, SubscriptionCancelled);

                await tcs.Task.WithCancellation(cancellationToken).ConfigureAwait(false);
            }


            /// <inheritdoc />
            public override ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken) {
                var result = ChannelExtensions.CreateTagIdentifierChannel(-1);

                result.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var hubChannel = await _client.TagValues.GetSnapshotTagValueChannelSubscriptionsAsync(
                        _adapterId,
                        ct
                    ).ConfigureAwait(false);

                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
                }, true, cancellationToken);

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
                    _adapterId,
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
                    _adapterId,
                    tagsToRemove,
                    cancellationToken
                ).ConfigureAwait(false);
            }


            /// <inheritdoc/>
            protected override void Dispose(bool disposing) {
                if (disposing) {
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
