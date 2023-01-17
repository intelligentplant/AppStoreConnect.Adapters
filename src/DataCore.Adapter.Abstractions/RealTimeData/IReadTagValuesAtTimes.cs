using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading tag values at specific time stamps from an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.ReadTagValuesAtTimes,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ReadTagValuesAtTimes),
        Description = nameof(AbstractionsResources.Description_ReadTagValuesAtTimes)
    )]
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the values for the requested tags. 
        /// </returns>
        /// <remarks>
        ///   The adapter should return the raw samples at or immediately before each requested 
        ///   timestamp. It should not interpolate values.
        /// </remarks>
        IAsyncEnumerable<TagValueQueryResult> ReadTagValuesAtTimes(
            IAdapterCallContext context, 
            ReadTagValuesAtTimesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
