using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IReadTagValueAnnotations"/>.
    /// </summary>
    internal class ReadTagValueAnnotationsWrapper : AdapterFeatureWrapper<IReadTagValueAnnotations>, IReadTagValueAnnotations {

        /// <summary>
        /// Creates a new <see cref="ReadTagValueAnnotationsWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal ReadTagValueAnnotationsWrapper(AdapterCore adapter, IReadTagValueAnnotations innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<TagValueAnnotationQueryResult> IReadTagValueAnnotations.ReadAnnotations(IAdapterCallContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            return ServerStreamAsync(context, request, InnerFeature.ReadAnnotations, cancellationToken);
        }


        /// <inheritdoc/>
        Task<TagValueAnnotationExtended?> IReadTagValueAnnotations.ReadAnnotation(IAdapterCallContext context, ReadAnnotationRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.ReadAnnotation, cancellationToken);
        }

    }

}
