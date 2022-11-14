using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.Events {

    /// <summary>
    /// Wrapper for <see cref="IWriteEventMessages"/>.
    /// </summary>
    internal class WriteEventMessagesWrapper : AdapterFeatureWrapper<IWriteEventMessages>, IWriteEventMessages {

        /// <summary>
        /// Creates a new <see cref="WriteEventMessagesWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal WriteEventMessagesWrapper(AdapterCore adapter, IWriteEventMessages innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        IAsyncEnumerable<WriteEventMessageResult> IWriteEventMessages.WriteEventMessages(IAdapterCallContext context, WriteEventMessagesRequest request, IAsyncEnumerable<WriteEventMessageItem> channel, CancellationToken cancellationToken) {
            return DuplexStreamAsync(context, request, channel, InnerFeature.WriteEventMessages, cancellationToken);
        }

    }

}
