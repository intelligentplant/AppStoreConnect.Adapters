using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.Hubs {

    // Adds hub methods for querying adapter tag annotations.

    public partial class AdapterHub {

        /// <summary>
        /// Reads a single tag value annotation.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The read request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching annotation.
        /// </returns>
        public async Task<TagValueAnnotationExtended> ReadAnnotation(string adapterId, ReadAnnotationRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.ReadAnnotation(AdapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads tag value annotations.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The read request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will return the matching annotations.
        /// </returns>
        public async Task<ChannelReader<TagValueAnnotationQueryResult>> ReadAnnotations(string adapterId, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.ReadAnnotations(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Creates a tag value annotation.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The create request.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        public async Task<WriteTagValueAnnotationResult> CreateAnnotation(string adapterId, CreateAnnotationRequest request) {
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.CreateAnnotation(AdapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Updates a tag value annotation.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The update request.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        public async Task<WriteTagValueAnnotationResult> UpdateAnnotation(string adapterId, UpdateAnnotationRequest request) {
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.UpdateAnnotation(AdapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a tag value annotation.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The delete request.
        /// </param>
        /// <returns>
        ///   The result of the operation.
        /// </returns>
        public async Task<WriteTagValueAnnotationResult> DeleteAnnotation(string adapterId, DeleteAnnotationRequest request) {
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return await adapter.Feature.DeleteAnnotation(AdapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }

    }
}
