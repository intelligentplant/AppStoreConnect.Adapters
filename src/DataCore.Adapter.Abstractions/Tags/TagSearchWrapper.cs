using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Wrapper for <see cref="ITagSearch"/>.
    /// </summary>
    internal class TagSearchWrapper : AdapterFeatureWrapper<ITagSearch>, ITagSearch {

        /// <summary>
        /// Creates a new <see cref="TagSearchWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal TagSearchWrapper(AdapterCore adapter, ITagSearch innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<AdapterProperty> ITagInfo.GetTagProperties(IAdapterCallContext context, GetTagPropertiesRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.GetTagProperties, cancellationToken);
        }


        /// <inheritdoc/>
        IAsyncEnumerable<TagDefinition> ITagInfo.GetTags(IAdapterCallContext context, GetTagsRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.GetTags, cancellationToken);
        }


        /// <inheritdoc/>
        IAsyncEnumerable<TagDefinition> ITagSearch.FindTags(IAdapterCallContext context, FindTagsRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.FindTags, cancellationToken);
        }

    }

}
