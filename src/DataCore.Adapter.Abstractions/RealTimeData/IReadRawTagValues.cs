using System.Collections.Generic;
using System.Threading;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading raw tag values from an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.ReadRawTagValues,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_ReadRawTagValues),
        Description = nameof(AbstractionsResources.Description_ReadRawTagValues)
    )]
    public interface IReadRawTagValues : IAdapterFeature {

        /// <summary>
        /// Reads raw data from the adapter.
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
        ///   If the <see cref="ReadRawTagValuesRequest.SampleCount"/> is less than one, this should be 
        ///   interpreted as meaning that the caller is requesting as many samples inside the time 
        ///   range as possible. The adapter can apply its own maximum sample count to the queries it 
        ///   receives.
        /// </remarks>
        IAsyncEnumerable<TagValueQueryResult> ReadRawTagValues(
            IAdapterCallContext context, 
            ReadRawTagValuesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
