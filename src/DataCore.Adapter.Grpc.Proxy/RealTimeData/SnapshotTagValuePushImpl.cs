using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        public SnapshotTagValuePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public async Task<ISnapshotTagValueSubscription> Subscribe(IAdapterCallContext context, CancellationToken cancellationToken) {
            ISnapshotTagValueSubscription result = new SnapshotTagValueSubscription(this, CreateClient<TagValuesService.TagValuesServiceClient>());
            try {
                await result.StartAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch {
                await result.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            return result;
        }


        private class SnapshotTagValueSubscription : Adapter.RealTimeData.SnapshotTagValueSubscription {

            private readonly SnapshotTagValuePushImpl _feature;

            private readonly TagValuesService.TagValuesServiceClient _client;

            private readonly HashSet<string> _tags = new HashSet<string>();

            public override int Count {
                get { return _tags.Count; }
            }


            public SnapshotTagValueSubscription(SnapshotTagValuePushImpl feature, TagValuesService.TagValuesServiceClient client) {
                _feature = feature;
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

                    var grpcRequest = new CreateSnapshotPushChannelRequest() {
                        AdapterId = _feature.AdapterId
                    };
                    grpcRequest.Tags.AddRange(tags);

                    try {
                        var grpcResponse = _client.CreateSnapshotPushChannel(grpcRequest, _feature.GetCallOptions(context, ct));
                        // The service will always send us an initial value, even if we didn't 
                        // include any values in the initial subscribe request.
                        await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false);
                        tcs.TrySetResult(0);

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
                    }
                    catch (OperationCanceledException) {
                        tcs.TrySetCanceled();
                        throw;
                    }
                    catch (Exception e) {
                        tcs.TrySetException(e);
                        throw;
                    }
                }, false, _feature.TaskScheduler, SubscriptionCancelled);

                await tcs.Task.WithCancellation(cancellationToken).ConfigureAwait(false);
            }


            public override System.Threading.Channels.ChannelReader<Adapter.RealTimeData.TagIdentifier> GetTags(IAdapterCallContext context, CancellationToken cancellationToken) {
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
                            await ch.WriteAsync(Adapter.RealTimeData.TagIdentifier.Create(
                                grpcResponse.ResponseStream.Current.Id,
                                grpcResponse.ResponseStream.Current.Name
                            ), ct).ConfigureAwait(false);
                        }
                    }
                    finally {
                        grpcResponse.Dispose();
                    }
                }, true, _feature.TaskScheduler, cancellationToken);

                return result;
            }


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

                var grpcRequest = new AddTagsToSnapshotPushChannelRequest() {
                    AdapterId = _feature.AdapterId
                };
                grpcRequest.Tags.AddRange(tagsToAdd);

                var grpcResponse = _client.AddTagsToSnapshotPushChannelAsync(grpcRequest, _feature.GetCallOptions(context, cancellationToken));
                var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

                return result.Count;
            }


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

                var grpcRequest = new RemoveTagsFromSnapshotPushChannelRequest() {
                    AdapterId = _feature.AdapterId
                };
                grpcRequest.Tags.AddRange(tagsToRemove);

                var grpcResponse = _client.RemoveTagsFromSnapshotPushChannelAsync(grpcRequest, _feature.GetCallOptions(context, cancellationToken));
                var result = await grpcResponse.ResponseAsync.ConfigureAwait(false);

                return result.Count;
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
