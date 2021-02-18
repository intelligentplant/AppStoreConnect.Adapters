using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="ITagSearch"/> (and <see cref="ITagInfo"/>).
    /// </summary>
    internal class TagSearchImpl : ProxyAdapterFeature, ITagSearch {

        /// <summary>
        /// Creates a new <see cref="TagSearchImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public TagSearchImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async Task<ChannelReader<TagDefinition>> FindTags(IAdapterCallContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            SignalRAdapterProxy.ValidateObject(request); 
            
            var client = GetClient();
            var hubChannel = await client.TagSearch.FindTagsAsync(
                AdapterId, 
                request, 
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, BackgroundTaskService, cancellationToken);

            return result;
        }


        /// <inheritdoc />
        public async Task<ChannelReader<TagDefinition>> GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            SignalRAdapterProxy.ValidateObject(request); 
            
            var client = GetClient();
            var hubChannel = await client.TagSearch.GetTagsAsync(
                AdapterId, 
                request, 
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateTagDefinitionChannel(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, BackgroundTaskService, cancellationToken);

            return result;
        }


        /// <inheritdoc />
        public async Task<ChannelReader<AdapterProperty>> GetTagProperties(IAdapterCallContext context, GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            
            SignalRAdapterProxy.ValidateObject(request); 
            
            var client = GetClient();
            var hubChannel = await client.TagSearch.GetTagPropertiesAsync(
                AdapterId, 
                request, 
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateChannel<AdapterProperty>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, BackgroundTaskService, cancellationToken);

            return result;
        }

    }
}
