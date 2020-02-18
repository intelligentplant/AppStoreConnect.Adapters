using System.Threading;
using System.Threading.Channels;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading tag values at specific time stamps from an adapter.
    /// </summary>
    public interface IReadTagValuesAtTimes : IAdapterFeature {

        /// <summary>
        /// Reads values from the adapter with specific time stamps.
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
        ///   A channel containing the values for the requested tags. The adapter can decide if it 
        ///   will interpolate a tag value using the closest raw samples to a requested time stamp, 
        ///   or if it will repeat the previous raw value before the time stamp.
        /// </returns>
        ChannelReader<TagValueQueryResult> ReadTagValuesAtTimes(IAdapterCallContext context, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken);

    }
}
