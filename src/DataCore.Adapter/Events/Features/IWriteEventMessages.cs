using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Models;

namespace DataCore.Adapter.Events.Features {

    /// <summary>
    /// Feature that allows event messages to be written to an adapter.
    /// </summary>
    public interface IWriteEventMessages : IAdapterFeature {

        /// <summary>
        /// Writes event messages to an adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   A request object describing the event messages to be written.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the registration operation. 
        /// </param>
        /// <returns>
        ///   A response object describing if the write operation was successful or not.
        /// </returns>
        Task<WriteEventMessagesResult> WriteEventMessages(IAdapterCallContext context, WriteEventMessagesRequest request, CancellationToken cancellationToken);

    }
}
