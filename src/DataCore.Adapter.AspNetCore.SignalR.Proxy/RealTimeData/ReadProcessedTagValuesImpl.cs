using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

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
        public ReadProcessedTagValuesImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<DataFunctionDescriptor> GetSupportedDataFunctions(
            IAdapterCallContext context, 
            GetSupportedDataFunctionsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.TagValues.GetSupportedDataFunctionsAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ProcessedTagValueQueryResult> ReadProcessedTagValues(
            IAdapterCallContext context, 
            ReadProcessedTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            await foreach (var item in client.TagValues.ReadProcessedTagValuesAsync(
                AdapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }
        
    }

}
