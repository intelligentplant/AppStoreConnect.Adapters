using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Feature for performing tag searches on an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Tags.TagSearch,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_TagSearch),
        Description = nameof(AbstractionsResources.Description_TagSearch)
    )]
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
        IAsyncEnumerable<TagDefinition> FindTags(
            IAdapterCallContext context, 
            FindTagsRequest request, 
            CancellationToken cancellationToken
        );

    }
}
