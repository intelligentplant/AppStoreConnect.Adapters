using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading processed (aggregated) tag values from an adapter.
    /// </summary>
    public interface IReadProcessedTagValues : IAdapterFeature {

        /// <summary>
        /// Gets information about the data functions that can be specified when calling 
        /// <see cref="ReadProcessedTagValues"/>.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the available data functions.
        /// </returns>
        ChannelReader<DataFunctionDescriptor> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Reads processed (aggregated) data from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The data query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel that will emit the values for the requested tags.
        /// </returns>
        ChannelReader<ProcessedTagValueQueryResult> ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken);

    }
}
