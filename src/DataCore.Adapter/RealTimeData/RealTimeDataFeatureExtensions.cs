using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace DataCore.Adapter.RealTimeData {
    /// <summary>
    /// Extension methods for real-time-data-related features.
    /// </summary>
    public static class RealTimeDataFeatureExtensions {

        /// <summary>
        /// Writes a collection of snapshot tag values to an adapter.
        /// </summary>
        /// <param name="feature">
        ///   The <see cref="IWriteSnapshotTagValues"/> feature to write the values to.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="values">
        ///   The event messages to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{T}"/> that will contain a write result for each item read from 
        ///   the input <paramref name="values"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<IEnumerable<WriteTagValueResult>> WriteSnapshotTagValues(
            this IWriteSnapshotTagValues feature, 
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IEnumerable<WriteTagValueItem> values,
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            var result = new List<WriteTagValueResult>(values.Count());
            await foreach (var item in feature.WriteSnapshotTagValues(context, request, values.PublishToChannel().ReadAllAsync(cancellationToken), cancellationToken).ConfigureAwait(false)) {
                result.Add(item);
            }

            return result;
        }


        /// <summary>
        /// Writes a collection of historical tag values to an adapter.
        /// </summary>
        /// <param name="feature">
        ///   The <see cref="IWriteHistoricalTagValues"/> feature to write the values to.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="values">
        ///   The event messages to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{T}"/> that will contain a write result for each item read from 
        ///   the input <paramref name="values"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<IEnumerable<WriteTagValueResult>> WriteHistoricalTagValues(
            this IWriteHistoricalTagValues feature, 
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IEnumerable<WriteTagValueItem> values,
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            var result = new List<WriteTagValueResult>(values.Count());
            await foreach (var item in feature.WriteHistoricalTagValues(context, request, values.PublishToChannel().ReadAllAsync(cancellationToken), cancellationToken).ConfigureAwait(false)) {
                result.Add(item);
            }

            return result;
        }

    }
}
