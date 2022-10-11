using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.RealTimeData;
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadAnnotationActivity(adapter.Adapter.Descriptor.Id, request)) {
                var result = await adapter.Feature.ReadAnnotation(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
                activity.SetResponseItemCountTag(result == null ? 0 : 1);
                return result;
            }
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (var activity = Telemetry.ActivitySource.StartReadAnnotationsActivity(adapter.Adapter.Descriptor.Id, request)) {
                long itemCount = 0;

                try {
                    await foreach (var item in adapter.Feature.ReadAnnotations(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                        ++itemCount;
                        yield return item;
                    }
                }
                finally {
                    activity.SetResponseItemCountTag(itemCount);
                }
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartCreateAnnotationActivity(adapter.Adapter.Descriptor.Id, request)) {
                return await adapter.Feature.CreateAnnotation(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
            }
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartUpdateAnnotationActivity(adapter.Adapter.Descriptor.Id, request)) {
                return await adapter.Feature.UpdateAnnotation(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
            }
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var cancellationToken = Context.ConnectionAborted;
            var adapter = await ResolveAdapterAndFeature<IWriteTagValueAnnotations>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);

            using (Telemetry.ActivitySource.StartDeleteAnnotationActivity(adapter.Adapter.Descriptor.Id, request)) {
                return await adapter.Feature.DeleteAnnotation(adapterCallContext, request, cancellationToken).ConfigureAwait(false);
            }
        }

    }
}
