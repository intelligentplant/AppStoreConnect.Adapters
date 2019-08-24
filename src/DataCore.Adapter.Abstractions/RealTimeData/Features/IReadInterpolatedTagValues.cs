using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Features {

    /// <summary>
    /// Feature for reading interpolated tag values from an adapter.
    /// </summary>
    public interface IReadInterpolatedTagValues : IAdapterFeature {

        /// <summary>
        /// Reads interpolated data from the adapter.
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
        ///   A channel containing the values for the requested tags.
        /// </returns>
        ChannelReader<TagValueQueryResult> ReadInterpolatedTagValues(IAdapterCallContext context, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken);

    }
}
