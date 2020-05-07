using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for requesting information about tags.
    /// </summary>
    public interface ITagInfo : IAdapterFeature {

        /// <summary>
        /// Gets the definitions of the bespoke properties that can are included in <see cref="TagDefinition"/> 
        /// objects returned by the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The available tag properties.
        /// </returns>
        ChannelReader<AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            GetTagPropertiesRequest request, 
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Gets tags by ID or name.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tag definitions.
        /// </returns>
        ChannelReader<TagDefinition> GetTags(
            IAdapterCallContext context, 
            GetTagsRequest request, 
            CancellationToken cancellationToken
        );

    }
}
