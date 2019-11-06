using System.Threading;
using System.Threading.Channels;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for requesting information about tags.
    /// </summary>
    public interface ITagInfo : IAdapterFeature {

        /// <summary>
        /// Gets tags by ID or name.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The search filter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tag definitions.
        /// </returns>
        ChannelReader<TagDefinition> GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken);

    }
}
