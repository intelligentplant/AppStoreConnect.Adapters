using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

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
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to register the operation with. Specify 
        ///   <see langword="null"/> to use the default scheduler.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ChannelReader{T}"/> that will emit a write result for each item read from 
        ///   the input <paramref name="events"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="events"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<ChannelReader<WriteEventMessageResult>> WriteEventMessages(this IWriteEventMessages feature, IAdapterCallContext context, IEnumerable<WriteEventMessageItem> events, IBackgroundTaskService backgroundTaskService = null, CancellationToken cancellationToken = default) {
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
            }, true, backgroundTaskService, cancellationToken);

            return await feature.WriteEventMessages(context, channel, cancellationToken).ConfigureAwait(false);
        }

    }
}
