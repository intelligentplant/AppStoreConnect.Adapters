using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {

    /// <summary>
    /// Implements <see cref="IReadTagValuesAtTimes"/>.
    /// </summary>
    internal class ReadTagValuesAtTimesImpl : ProxyAdapterFeature, IReadTagValuesAtTimes {

        /// <summary>
        /// Creates a new <see cref="ReadTagValuesAtTimesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadTagValuesAtTimesImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<TagValueQueryResult> ReadTagValuesAtTimes(
            IAdapterCallContext context, 
            ReadTagValuesAtTimesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var clientResponse = await client.TagValues.ReadTagValuesAtTimesAsync(AdapterId, request, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    yield return item;
                }
            }
        }

    }

}
