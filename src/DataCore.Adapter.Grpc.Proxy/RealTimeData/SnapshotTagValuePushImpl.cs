using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="ISnapshotTagValuePush"/> implementation.
    /// </summary>
    internal class SnapshotTagValuePushImpl : ProxyAdapterFeature, ISnapshotTagValuePush {

        /// <summary>
        /// Creates a new <see cref="SnapshotTagValuePushImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public SnapshotTagValuePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public Task<ChannelReader<Adapter.RealTimeData.TagValueQueryResult>> Subscribe(
            IAdapterCallContext context, 
            CreateSnapshotTagValueSubscriptionRequest request, 
            CancellationToken cancellationToken
        ) {
            GrpcAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateTagValueChannel<Adapter.RealTimeData.TagValueQueryResult>(0);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagValuesService.TagValuesServiceClient>();

                var grpcRequest = new CreateSnapshotPushChannelRequest() {
                    AdapterId = AdapterId,
                    Tag = request.Tag,
                    PublishInterval = request.PublishInterval > TimeSpan.Zero
                        ? Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(request.PublishInterval)
                        : Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(TimeSpan.Zero)
                };

                if (request.Properties != null) {
                    foreach (var item in request.Properties) {
                        grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                    }
                }

                using (var grpcChannel = client.CreateSnapshotPushChannel(
                   grpcRequest,
                   GetCallOptions(context, ct)
                )) {
                    // Read event messages.
                    while (await grpcChannel.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
                        if (grpcChannel.ResponseStream.Current == null) {
                            continue;
                        }

                        await result.Writer.WriteAsync(grpcChannel.ResponseStream.Current.ToAdapterTagValueQueryResult(), ct).ConfigureAwait(false);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
