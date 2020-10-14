using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Events {
    /// <summary>
    /// Extension methods for event-related features.
    /// </summary>
    public static class EventFeatureExtensions {

        /// <summary>
        /// Writes a collection of event messages to an adapter.
        /// </summary>
        /// <param name="feature">
        ///   The <see cref="IWriteEventMessages"/> feature to write the messages to.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="events">
        ///   The event messages to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IEnumerable{T}"/> that will contain a write result for each item read from 
        ///   the input <paramref name="events"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="events"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<IEnumerable<WriteEventMessageResult>> WriteEventMessages(
            this IWriteEventMessages feature, 
            IAdapterCallContext context, 
            IEnumerable<WriteEventMessageItem> events,
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }

            var channel = ChannelExtensions.CreateEventMessageWriteChannel();
            channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                foreach (var item in events) {
                    if (!await ch.WaitToWriteAsync(ct).ConfigureAwait(false)) {
                        break;
                    }

                    ch.TryWrite(item);
                }
            }, true, feature.BackgroundTaskService, cancellationToken);

            var result = new List<WriteEventMessageResult>(events.Count());
            var outChannel = await feature.WriteEventMessages(context, channel, cancellationToken).ConfigureAwait(false);

            while (await outChannel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (outChannel.TryRead(out var item)) {
                    result.Add(item);
                }
            }

            return result;
        }

    }
}
