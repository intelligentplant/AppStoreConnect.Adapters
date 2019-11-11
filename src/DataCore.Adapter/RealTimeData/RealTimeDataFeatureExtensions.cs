using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using IntelligentPlant.BackgroundTasks;

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
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <returns>
        ///   A <see cref="ChannelReader{T}"/> that will emit a write result for each item read from 
        ///   the input <paramref name="values"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        public static ChannelReader<WriteTagValueResult> WriteSnapshotTagValues(this IWriteSnapshotTagValues feature, IAdapterCallContext context, IEnumerable<WriteTagValueItem> values, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
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
            }, true, scheduler, cancellationToken);

            return feature.WriteSnapshotTagValues(context, channel, cancellationToken);
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
        /// <param name="scheduler">
        ///   The background task service to use when writing values into the channel.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ChannelReader{T}"/> that will emit a write result for each item read from 
        ///   the input <paramref name="values"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        public static ChannelReader<WriteTagValueResult> WriteHistoricalTagValues(this IWriteHistoricalTagValues feature, IAdapterCallContext context, IEnumerable<WriteTagValueItem> values, IBackgroundTaskService scheduler = null, CancellationToken cancellationToken = default) {
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
            }, true, scheduler, cancellationToken);

            return feature.WriteHistoricalTagValues(context, channel, cancellationToken);
        }

    }
}
