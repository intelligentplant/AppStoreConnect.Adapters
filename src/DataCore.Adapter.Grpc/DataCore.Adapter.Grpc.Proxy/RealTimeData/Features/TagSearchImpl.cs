using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {
    internal class TagSearchImpl : ProxyAdapterFeature, ITagSearch {

        public TagSearchImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<Adapter.RealTimeData.Models.TagDefinition> FindTags(IAdapterCallContext context, Adapter.RealTimeData.Models.FindTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = CreateClient<TagSearchService.TagSearchServiceClient>();
                var grpcRequest = new FindTagsRequest() {
                    AdapterId = AdapterId,
                    Name = request.Name,
                    Description = request.Description,
                    Units = request.Units,
                    PageSize = request.PageSize,
                    Page = request.Page
                };
                if (request.Other?.Count > 0) {
                    foreach (var item in request.Other) {
                        grpcRequest.Other[item.Key] = item.Value;
                    }
                }

                var grpcResponse = client.FindTags(grpcRequest, cancellationToken: ct);
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

                var grpcResponse = client.GetTags(grpcRequest, cancellationToken: ct);
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
