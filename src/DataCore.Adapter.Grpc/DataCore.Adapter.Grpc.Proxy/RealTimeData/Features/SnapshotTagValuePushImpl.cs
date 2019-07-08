using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        public SnapshotTagValuePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CancellationToken cancellationToken) {
            var result = new SnapshotTagValueSubscription(this, CreateClient<TagValuesService.TagValuesServiceClient>());
            result.Start(context);
            return Task.FromResult<ISnapshotTagValueSubscription>(result);
        }


        private class SnapshotTagValueSubscription : ISnapshotTagValueSubscription {

            private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

            private readonly SnapshotTagValuePushImpl _feature;

            private readonly TagValuesService.TagValuesServiceClient _client;

            private readonly System.Threading.Channels.Channel<Adapter.RealTimeData.Models.TagValueQueryResult> _channel = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.Models.TagValueQueryResult>();

            private readonly HashSet<string> _tags = new HashSet<string>();

            public System.Threading.Channels.ChannelReader<Adapter.RealTimeData.Models.TagValueQueryResult> Reader { get { return _channel; } }

            public int Count {
                get { return _tags.Count; }
            }


            public SnapshotTagValueSubscription(SnapshotTagValuePushImpl feature, TagValuesService.TagValuesServiceClient client) {
                _feature = feature;
                _client = client;
            }


            public void Start(IAdapterCallContext context) {
                _channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                    string[] tags;
                    lock (_tags) {
                        tags = _tags.ToArray();
                    }

                    var grpcRequest = new CreateSnapshotPushChannelRequest() {
                        AdapterId = _feature.AdapterId
                    };
                    grpcRequest.Tags.AddRange(tags);

                    var grpcResponse = _client.CreateSnapshotPushChannel(grpcRequest, _feature.GetCallOptions(context, ct));
                    try {
                        while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                            if (grpcResponse.ResponseStream.Current == null) {
                                continue;
                            }
                            await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterTagValueQueryResult(), ct).ConfigureAwait(false);
                        }
                    }
                    finally {
                        grpcResponse.Dispose();
                    }
                }, false, _shutdownTokenSource.Token);
            }


            public System.Threading.Channels.ChannelReader<Adapter.RealTimeData.Models.TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken) {
                var result = ChannelExtensions.CreateTagIdentifierChannel();

                result.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var grpcRequest = new GetSnapshotPushChannelTagsRequest() {
                        AdapterId = _feature.AdapterId
                    };
                    
                    var grpcResponse = _client.GetSnapshotPushChannelTags(grpcRequest, _feature.GetCallOptions(context, ct));
                    try {
                        while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                            if (grpcResponse.ResponseStream.Current == null) {
                                continue;
                            }
                            await ch.WriteAsync(new Adapter.RealTimeData.Models.TagIdentifier(
                                grpcResponse.ResponseStream.Current.Id,
                                grpcResponse.ResponseStream.Current.Name
                            ), ct).ConfigureAwait(false);
                        }
                    }
                    finally {
                        grpcResponse.Dispose();
                    }
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

                var grpcRequest = new AddTagsToSnapshotPushChannelRequest() {
                    AdapterId = _feature.AdapterId
                };
                grpcRequest.Tags.AddRange(tagsToAdd);

                var grpcResponse = _client.AddTagsToSnapshotPushChannelAsync(grpcRequest, _feature.GetCallOptions(context, cancellationToken));
                var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

                return result.Count;
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

                var grpcRequest = new RemoveTagsFromSnapshotPushChannelRequest() {
                    AdapterId = _feature.AdapterId
                };
                grpcRequest.Tags.AddRange(tagsToRemove);

                var grpcResponse = _client.RemoveTagsFromSnapshotPushChannelAsync(grpcRequest, _feature.GetCallOptions(context, cancellationToken));
                var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

                return result.Count;
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
