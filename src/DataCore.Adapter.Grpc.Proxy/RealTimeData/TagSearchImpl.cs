using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="ITagSearch"/> (and <see cref="ITagInfo"/>) implementation.
    /// </summary>
    internal class TagSearchImpl : ProxyAdapterFeature, ITagSearch {

        /// <summary>
        /// Creates a new <see cref="TagSearchImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public TagSearchImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public ChannelReader<Adapter.RealTimeData.TagDefinition> FindTags(IAdapterCallContext context, Adapter.RealTimeData.FindTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagSearchService.TagSearchServiceClient>();
                var grpcRequest = new FindTagsRequest() {
                    AdapterId = AdapterId,
                    Name = request.Name ?? string.Empty,
                    Description = request.Description ?? string.Empty,
                    Units = request.Units ?? string.Empty,
                    PageSize = request.PageSize,
                    Page = request.Page,
                    Label = request.Label ?? string.Empty
                };
                if (request.Other?.Count > 0) {
                    foreach (var item in request.Other) {
                        grpcRequest.Other[item.Key] = item.Value;
                    }
                }

                var grpcResponse = client.FindTags(grpcRequest, GetCallOptions(context, ct));
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterTagDefinition(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }


        /// <inheritdoc />
        public ChannelReader<Adapter.RealTimeData.TagDefinition> GetTags(IAdapterCallContext context, Adapter.RealTimeData.GetTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagSearchService.TagSearchServiceClient>();
                var grpcRequest = new GetTagsRequest() {
                    AdapterId = AdapterId
                };
                grpcRequest.Tags.AddRange(request.Tags);

                var grpcResponse = client.GetTags(grpcRequest, GetCallOptions(context, ct));
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterTagDefinition(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }


        /// <inheritdoc />
        public ChannelReader<Common.AdapterProperty> GetTagProperties(IAdapterCallContext context, Adapter.RealTimeData.GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateChannel<Common.AdapterProperty>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagSearchService.TagSearchServiceClient>();
                var grpcRequest = new GetTagPropertiesRequest() {
                    AdapterId = AdapterId,
                    PageSize = request.PageSize,
                    Page = request.Page
                };

                var grpcResponse = client.GetTagProperties(grpcRequest, GetCallOptions(context, ct));
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterProperty(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }

    }
}
