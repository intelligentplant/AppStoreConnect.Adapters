using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Wrapper for <see cref="IWriteTagValueAnnotations"/>.
    /// </summary>
    internal class WriteTagValueAnnotationsWrapper : AdapterFeatureWrapper<IWriteTagValueAnnotations>, IWriteTagValueAnnotations {

        /// <summary>
        /// Creates a new <see cref="WriteTagValueAnnotationsWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal WriteTagValueAnnotationsWrapper(AdapterCore adapter, IWriteTagValueAnnotations innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        Task<WriteTagValueAnnotationResult> IWriteTagValueAnnotations.CreateAnnotation(IAdapterCallContext context, CreateAnnotationRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.CreateAnnotation, cancellationToken);
        }


        /// <inheritdoc/>
        Task<WriteTagValueAnnotationResult> IWriteTagValueAnnotations.UpdateAnnotation(IAdapterCallContext context, UpdateAnnotationRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.UpdateAnnotation, cancellationToken);
        }


        /// <inheritdoc/>
        Task<WriteTagValueAnnotationResult> IWriteTagValueAnnotations.DeleteAnnotation(IAdapterCallContext context, DeleteAnnotationRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.DeleteAnnotation, cancellationToken);
        }

    }

}
