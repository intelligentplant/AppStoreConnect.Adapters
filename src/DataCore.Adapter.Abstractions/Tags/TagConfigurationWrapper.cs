using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Wrapper for <see cref="ITagConfiguration"/>.
    /// </summary>
    internal class TagConfigurationWrapper : AdapterFeatureWrapper<ITagConfiguration>, ITagConfiguration {

        /// <summary>
        /// Creates a new <see cref="TagConfigurationWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal TagConfigurationWrapper(AdapterCore adapter, ITagConfiguration innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        Task<JsonElement> ITagConfiguration.GetTagSchemaAsync(IAdapterCallContext context, GetTagSchemaRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.GetTagSchemaAsync, cancellationToken);
        }


        /// <inheritdoc/>
        Task<TagDefinition> ITagConfiguration.CreateTagAsync(IAdapterCallContext context, CreateTagRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.CreateTagAsync, cancellationToken);
        }


        /// <inheritdoc/>
        Task<TagDefinition> ITagConfiguration.UpdateTagAsync(IAdapterCallContext context, UpdateTagRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.UpdateTagAsync, cancellationToken);
        }


        /// <inheritdoc/>
        Task<bool> ITagConfiguration.DeleteTagAsync(IAdapterCallContext context, DeleteTagRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.DeleteTagAsync, cancellationToken);
        }

    }

}
