using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        ///   <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<IEnumerable<WriteTagValueResult>> WriteSnapshotTagValues(
            this IWriteSnapshotTagValues feature, 
            IAdapterCallContext context, 
            IEnumerable<WriteTagValueItem> values,
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            var channel = ChannelExtensions.CreateTagValueWriteChannel();
            channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                foreach (var item in values) {
                    if (!await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        break;
                    }

                    ch.TryWrite(item);
                }
            }, true, feature.BackgroundTaskService, cancellationToken);

            var result = new List<WriteTagValueResult>(values.Count());
            var outChannel = await feature.WriteSnapshotTagValues(context, channel, cancellationToken).ConfigureAwait(false);

            while (await outChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (outChannel.TryRead(out var item)) {
                    result.Add(item);
                }
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
        ///   <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<IEnumerable<WriteTagValueResult>> WriteHistoricalTagValues(
            this IWriteHistoricalTagValues feature, 
            IAdapterCallContext context, 
            IEnumerable<WriteTagValueItem> values,
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            var channel = ChannelExtensions.CreateTagValueWriteChannel();
            channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                foreach (var item in values) {
                    if (!await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        break;
                    }

                    ch.TryWrite(item);
                }
            }, true, feature.BackgroundTaskService, cancellationToken);

            var result = new List<WriteTagValueResult>(values.Count());
            var outChannel = await feature.WriteHistoricalTagValues(context, channel, cancellationToken).ConfigureAwait(false);

            while (await outChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (outChannel.TryRead(out var item)) {
                    result.Add(item);
                }
            }

            return result;
        }

    }
}
