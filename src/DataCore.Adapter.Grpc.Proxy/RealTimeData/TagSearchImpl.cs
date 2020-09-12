using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        public Task<ChannelReader<Adapter.RealTimeData.TagDefinition>> FindTags(IAdapterCallContext context, Adapter.RealTimeData.FindTagsRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            GrpcAdapterProxy.ValidateObject(request);

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
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }

            var grpcResponse = client.FindTags(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
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
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc />
        public Task<ChannelReader<Adapter.RealTimeData.TagDefinition>> GetTags(IAdapterCallContext context, Adapter.RealTimeData.GetTagsRequest request, CancellationToken cancellationToken) {
            GrpcAdapterProxy.ValidateObject(request); 
            
            var client = CreateClient<TagSearchService.TagSearchServiceClient>();
            var grpcRequest = new GetTagsRequest() {
                AdapterId = AdapterId
            };
            grpcRequest.Tags.AddRange(request.Tags);

            var grpcResponse = client.GetTags(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
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
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc />
        public Task<ChannelReader<Common.AdapterProperty>> GetTagProperties(IAdapterCallContext context, Adapter.RealTimeData.GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            GrpcAdapterProxy.ValidateObject(request); 
            
            var client = CreateClient<TagSearchService.TagSearchServiceClient>();
            var grpcRequest = new GetTagPropertiesRequest() {
                AdapterId = AdapterId,
                PageSize = request.PageSize,
                Page = request.Page
            };

            var grpcResponse = client.GetTagProperties(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateChannel<Common.AdapterProperty>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
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
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
