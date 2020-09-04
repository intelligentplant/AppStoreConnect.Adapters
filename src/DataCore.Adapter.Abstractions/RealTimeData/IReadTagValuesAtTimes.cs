﻿using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Feature for reading tag values at specific time stamps from an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.RealTimeData.ReadTagValuesAtTimes,
        ResourceType = typeof(DataCoreAdapterAbstractionsResources),
        Name = nameof(DataCoreAdapterAbstractionsResources.DisplayName_ReadTagValuesAtTimes),
        Description = nameof(DataCoreAdapterAbstractionsResources.Description_ReadTagValuesAtTimes)
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
        ///   A channel containing the values for the requested tags. The adapter can decide if it 
        ///   will interpolate a tag value using the closest raw samples to a requested time stamp, 
        ///   or if it will repeat the previous raw value before the time stamp.
        /// </returns>
        Task<ChannelReader<TagValueQueryResult>> ReadTagValuesAtTimes(
            IAdapterCallContext context, 
            ReadTagValuesAtTimesRequest request, 
            CancellationToken cancellationToken
        );

    }
}
