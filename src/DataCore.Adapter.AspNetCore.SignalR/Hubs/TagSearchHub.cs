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

    // Adds hub methods for tag search queries.
    
    public partial class AdapterHub {

        /// <summary>
        /// Performs a tag search.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tags.
        /// </returns>
        public async Task<ChannelReader<TagDefinition>> FindTags(string adapterId, FindTagsRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<ITagSearch>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.FindTags(AdapterCallContext, request, cancellationToken);
        }


        /// <summary>
        /// Gets tags by ID or name.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching tags.
        /// </returns>
        public async Task<ChannelReader<TagDefinition>> GetTags(string adapterId, GetTagsRequest request, CancellationToken cancellationToken) {
            var adapter = await ResolveAdapterAndFeature<ITagSearch>(adapterId, cancellationToken).ConfigureAwait(false);
            ValidateObject(request);
            return adapter.Feature.GetTags(AdapterCallContext, request, cancellationToken);
        }

    }
}
