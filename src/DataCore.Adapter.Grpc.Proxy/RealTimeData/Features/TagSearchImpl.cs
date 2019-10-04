using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.RealTimeData.Features;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class TagSearchImpl : ProxyAdapterFeature, ITagSearch {

        public TagSearchImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.RealTimeData.Models.TagDefinition> FindTags(IAdapterCallContext context, Adapter.RealTimeData.Models.FindTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel();

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
            }, true, cancellationToken);

            return result;
        }


        public ChannelReader<Adapter.RealTimeData.Models.TagDefinition> GetTags(IAdapterCallContext context, Adapter.RealTimeData.Models.GetTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel();

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
            }, true, cancellationToken);

            return result;
        }

    }
}
