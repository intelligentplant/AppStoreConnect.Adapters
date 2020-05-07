using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for creating, updating, and deleting annotations on tag values.
    /// </summary>
    public interface IWriteTagValueAnnotations : IAdapterFeature {

        /// <summary>
        /// Creates a new annotation.
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
        ///   A task that will return the result of the operation.
        /// </returns>
        Task<WriteTagValueAnnotationResult> CreateAnnotation(
            IAdapterCallContext context, 
            CreateAnnotationRequest request, 
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Updates an existing annotation.
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
        ///   A task that will return the result of the operation.
        /// </returns>
        Task<WriteTagValueAnnotationResult> UpdateAnnotation(
            IAdapterCallContext context, 
            UpdateAnnotationRequest request, 
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Deletes an annotation.
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
        ///   A task that will return the result of the operation.
        /// </returns>
        Task<WriteTagValueAnnotationResult> DeleteAnnotation(
            IAdapterCallContext context, 
            DeleteAnnotationRequest request, 
            CancellationToken cancellationToken
        );

    }

}
