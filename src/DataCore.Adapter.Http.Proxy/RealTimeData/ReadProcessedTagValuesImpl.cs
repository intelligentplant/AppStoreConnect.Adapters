using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {

    /// <summary>
    /// Implements <see cref="IReadProcessedTagValues"/>.
    /// </summary>
    internal class ReadProcessedTagValuesImpl : ProxyAdapterFeature, IReadProcessedTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadProcessedTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadProcessedTagValuesImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<DataFunctionDescriptor> GetSupportedDataFunctions(
            IAdapterCallContext context, 
            GetSupportedDataFunctionsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var clientResponse = await client.TagValues.GetSupportedDataFunctionsAsync(AdapterId, request, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    yield return item;
                }
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ProcessedTagValueQueryResult> ReadProcessedTagValues(
            IAdapterCallContext context, 
            ReadProcessedTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var clientResponse = await client.TagValues.ReadProcessedTagValuesAsync(AdapterId, request, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    yield return item;
                }
            }
        }

    }

}
