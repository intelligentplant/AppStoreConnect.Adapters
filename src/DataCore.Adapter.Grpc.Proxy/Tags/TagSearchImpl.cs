using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Grpc.Proxy.Tags.Features {

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
        public async IAsyncEnumerable<Adapter.Tags.TagDefinition> FindTags(
            IAdapterCallContext context, 
            Adapter.Tags.FindTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<TagSearchService.TagSearchServiceClient>();
            var grpcRequest = new FindTagsRequest() {
                AdapterId = AdapterId,
                Name = request.Name ?? string.Empty,
                Description = request.Description ?? string.Empty,
                Units = request.Units ?? string.Empty,
                PageSize = request.PageSize,
                Page = request.Page,
                Label = request.Label ?? string.Empty,
                ResultFields = (int) request.ResultFields
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

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcResponse = client.FindTags(grpcRequest, GetCallOptions(context, ctSource.Token))) {
                while (await grpcResponse.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterTagDefinition();
                }
            }
        }


        /// <inheritdoc />
        public async IAsyncEnumerable<Adapter.Tags.TagDefinition> GetTags(
            IAdapterCallContext context, 
            Adapter.Tags.GetTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<TagSearchService.TagSearchServiceClient>();
            var grpcRequest = new GetTagsRequest() {
                AdapterId = AdapterId
            };
            grpcRequest.Tags.AddRange(request.Tags);

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcResponse = client.GetTags(grpcRequest, GetCallOptions(context, ctSource.Token))) {
                while (await grpcResponse.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterTagDefinition();
                }
            }
        }


        /// <inheritdoc />
        public async IAsyncEnumerable<Common.AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            Adapter.Tags.GetTagPropertiesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<TagSearchService.TagSearchServiceClient>();
            var grpcRequest = new GetTagPropertiesRequest() {
                AdapterId = AdapterId,
                PageSize = request.PageSize,
                Page = request.Page
            };

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcResponse = client.GetTagProperties(grpcRequest, GetCallOptions(context, ctSource.Token))) {
                while (await grpcResponse.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    if (grpcResponse.ResponseStream.Current == null) {
                        continue;
                    }
                    yield return grpcResponse.ResponseStream.Current.ToAdapterProperty();
                }
            }
        }

    }
}
