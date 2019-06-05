using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.RealTimeData.Features {

    /// <summary>
    /// Feature for reading tag value annotations from an adapter.
    /// </summary>
    public interface IReadTagValueAnnotations : IAdapterFeature {

        /// <summary>
        /// Reads annotations from the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The annotation query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel containing the annotations for the requested tags.
        /// </returns>
        ChannelReader<TagValueAnnotationQueryResult> ReadTagValueAnnotations(IAdapterCallContext context, ReadAnnotationsRequest request, CancellationToken cancellationToken);

    }
}
