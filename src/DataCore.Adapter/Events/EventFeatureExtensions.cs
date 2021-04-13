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
        /// <param name="request">
        ///   The write request.
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
            WriteEventMessagesRequest request,
            IEnumerable<WriteEventMessageItem> events,
            CancellationToken cancellationToken = default
        ) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }

            return await feature.WriteEventMessages(context, request, events.ToAsyncEnumerable(cancellationToken), cancellationToken).ToEnumerable(events.Count(), cancellationToken).ConfigureAwait(false);
        }

    }
}
