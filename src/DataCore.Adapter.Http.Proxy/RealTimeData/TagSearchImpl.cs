using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {
    /// <summary>
    /// Implements <see cref="ITagSearch"/>.
    /// </summary>
    internal class TagSearchImpl : ProxyAdapterFeature, ITagSearch {

        /// <summary>
        /// Creates a new <see cref="TagSearchImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public TagSearchImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public Task<ChannelReader<TagDefinition>> FindTags(IAdapterCallContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            HttpAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagSearch.FindTagsAsync(AdapterId, request, context?.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc />
        public Task<ChannelReader<TagDefinition>> GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            HttpAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagSearch.GetTagsAsync(AdapterId, request, context?.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        /// <inheritdoc />
        public Task<ChannelReader<AdapterProperty>> GetTagProperties(IAdapterCallContext context, GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            HttpAdapterProxy.ValidateObject(request);

            var result = ChannelExtensions.CreateChannel<AdapterProperty>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagSearch.GetTagPropertiesAsync(AdapterId, request, context?.ToRequestMetadata(), ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
