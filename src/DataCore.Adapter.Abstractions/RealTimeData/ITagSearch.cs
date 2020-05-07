using System.Threading;
using System.Threading.Channels;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for performing tag searches on an adapter.
    /// </summary>
    public interface ITagSearch : IAdapterFeature, ITagInfo {

        /// <summary>
        /// Performs a tag search.
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
        ///   A channel that the search results can be read from.
        /// </returns>
        ChannelReader<TagDefinition> FindTags(
            IAdapterCallContext context, 
            FindTagsRequest request, 
            CancellationToken cancellationToken
        );

    }
}
