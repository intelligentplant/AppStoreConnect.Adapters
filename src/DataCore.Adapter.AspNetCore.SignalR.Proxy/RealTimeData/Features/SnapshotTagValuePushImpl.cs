using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

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
        public Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CancellationToken cancellationToken) {
            var result = new SnapshotTagValueSubscription(
                AdapterId,
                GetClient()
            );
            result.Start();
            return Task.FromResult<ISnapshotTagValueSubscription>(result);
        }

        /// <summary>
        /// <see cref="ISnapshotTagValueSubscription"/> implementation for the 
        /// <see cref="ISnapshotTagValuePush"/> feature.
        /// </summary>
        private class SnapshotTagValueSubscription : ISnapshotTagValueSubscription {

            /// <summary>
            /// Fires when the subscription is disposed.
            /// </summary>
            private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

            /// <summary>
            /// The adapter ID for the subscription.
            /// </summary>
            private readonly string _adapterId;

            /// <summary>
            /// The underlying adapter SignalR client.
            /// </summary>
            private readonly AdapterSignalRClient _client;

            /// <summary>
            /// The subscription channel.
            /// </summary>
            private readonly Channel<TagValueQueryResult> _channel = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>(-1);

            /// <summary>
            /// The tags that have been added to the subscription.
            /// </summary>
            private readonly HashSet<string> _tags = new HashSet<string>();

            /// <inheritdoc />
            public ChannelReader<TagValueQueryResult> Reader { get { return _channel; } }

            /// <inheritdoc />
            public int Count {
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


            /// <summary>
            /// Starts the subscription.
            /// </summary>
            public void Start() {
                _channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                    string[] tags;
                    lock (_tags) {
                        tags = _tags.ToArray();
                    }

                    var hubChannel = await _client.TagValues.CreateSnapshotTagValueChannelAsync(
                        _adapterId,
                        tags,
                        ct
                    ).ConfigureAwait(false);

                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
                }, true, _shutdownTokenSource.Token);
            }


            /// <inheritdoc />
            public ChannelReader<TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken) {
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
            public async Task<int> AddTagsToSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
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
            public async Task<int> RemoveTagsFromSubscription(IAdapterCallContext context, IEnumerable<string> tagNamesOrIds, CancellationToken cancellationToken) {
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
            public void Dispose() {
                _shutdownTokenSource.Cancel();
                _shutdownTokenSource.Dispose();
                lock (_tags) {
                    _tags.Clear();
                }
                _channel.Writer.TryComplete();
            }

        }
    }
}
