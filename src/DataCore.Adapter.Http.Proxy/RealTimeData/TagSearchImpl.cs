using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData;

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
        public ChannelReader<TagDefinition> FindTags(IAdapterCallContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagSearch.FindTagsAsync(AdapterId, request, context?.User, ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }

        /// <inheritdoc />
        public ChannelReader<TagDefinition> GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var client = GetClient();
                var clientResponse = await client.TagSearch.GetTagsAsync(AdapterId, request, context?.User, ct).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    if (await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        ch.TryWrite(item);
                    }
                }
            }, true, TaskScheduler, cancellationToken);

            return result;
        }
    }
}
