using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading tag value annotations from an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.ReadAnnotations,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ReadTagValueAnnotations),
        Description = nameof(AbstractionsResources.Description_ReadTagValueAnnotations)
    )]
    public interface IReadTagValueAnnotations : IAdapterFeature {

        /// <summary>
        /// Reads annotations from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The annotation query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel containing the annotations for the requested tags.
        /// </returns>
        Task<ChannelReader<TagValueAnnotationQueryResult>> ReadAnnotations(
            IAdapterCallContext context, 
            ReadAnnotationsRequest request, 
            CancellationToken cancellationToken
        );

        /// <summary>
        /// Gets an annotation from the adapter by ID.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The annotation query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The requested annotation.
        /// </returns>
        Task<TagValueAnnotationExtended?> ReadAnnotation(
            IAdapterCallContext context, 
            ReadAnnotationRequest request, 
            CancellationToken cancellationToken
        );

    }
}
