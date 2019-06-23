using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        public SnapshotTagValuePushImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        public async Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CancellationToken cancellationToken) {
            var result = new SnapshotTagValueSubscription(
                AdapterId,
                await this.GetTagValuesHubConnection(cancellationToken).ConfigureAwait(false)
            );
            result.Start();
            return result;
        }


        private class SnapshotTagValueSubscription : ISnapshotTagValueSubscription {

            private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

            private readonly string _adapterId;

            private readonly HubConnection _hubConnection;

            private readonly Channel<TagValueQueryResult> _channel = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            private readonly HashSet<string> _tags = new HashSet<string>();

            public ChannelReader<TagValueQueryResult> Reader { get { return _channel; } }

            public int Count {
                get { return _tags.Count; }
            }


            public SnapshotTagValueSubscription(string adapterId, HubConnection hubConnection) {
                _adapterId = adapterId;
                _hubConnection = hubConnection;
            }


            public void Start() {
                _channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                    string[] tags;
                    lock (_tags) {
                        tags = _tags.ToArray();
                    }

                    var hubChannel = await _hubConnection.StreamAsChannelAsync<TagValueQueryResult>(
                        "CreateSnapshotTagValueChannel",
                        _adapterId,
                        tags,
                        ct
                    ).ConfigureAwait(false);

                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
                }, false, _shutdownTokenSource.Token);
            }


            public ChannelReader<TagIdentifier> GetTags(CancellationToken cancellationToken) {
                var result = ChannelExtensions.CreateTagIdentifierChannel();

                result.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var hubChannel = await _hubConnection.StreamAsChannelAsync<TagIdentifier>(
                        "GetSnapshotTagValueChannelSubscriptions",
                        _adapterId,
                        ct
                    ).ConfigureAwait(false);

                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
                }, true, cancellationToken);

                return result;
            }


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

                return await _hubConnection.InvokeAsync<int>(
                    "AddTagsToSnapshotTagValueChannel",
                    _adapterId,
                    tagsToAdd,
                    cancellationToken
                ).ConfigureAwait(false);
            }


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

                return await _hubConnection.InvokeAsync<int>(
                    "RemoveTagsFromSnapshotTagValueChannel",
                    _adapterId,
                    tagsToRemove,
                    cancellationToken
                ).ConfigureAwait(false);
            }


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
