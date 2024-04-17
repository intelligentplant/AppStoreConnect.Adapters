using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
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
        /// <returns>
        ///   The matching annotation.
        /// </returns>
        public async Task<TagValueAnnotationExtended?> ReadAnnotation(string adapterId, ReadAnnotationRequest request) {
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            return await adapter.Feature.ReadAnnotation(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
        public async IAsyncEnumerable<TagValueAnnotationQueryResult> ReadAnnotations(
            string adapterId, 
            ReadAnnotationsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            await foreach (var item in adapter.Feature.ReadAnnotations(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            return await adapter.Feature.CreateAnnotation(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            return await adapter.Feature.UpdateAnnotation(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
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
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            return await adapter.Feature.DeleteAnnotation(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
        }

    }
}
