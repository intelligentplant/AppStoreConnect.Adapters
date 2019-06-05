using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub for querying adapter tag annotations.
    /// </summary>
    public class TagAnnotationsHub : AdapterHubBase {

        /// <summary>
        /// Creates a new <see cref="TagSearchHub"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The host information.
        /// </param>
        /// <param name="adapterCallContext">
        ///   The adapter call context describing the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   For accessing runtime adapters.
        /// </param>
        public TagAnnotationsHub(HostInfo hostInfo, IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor)
            : base(hostInfo, adapterCallContext, adapterAccessor) { }


        /// <summary>
        /// Reads tag value annotations.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The read request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching operations.
        /// </returns>
        public async Task<ChannelReader<TagValueAnnotationQueryResult>> ReadAnnotations(string adapterId, ReadAnnotationsRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<IReadTagValueAnnotations>(adapterId, cancellationToken).ConfigureAwait(false);
            return adapter.Feature.ReadTagValueAnnotations(AdapterCallContext, request, cancellationToken);
        }


    }
}
