using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {

    /// <summary>
    /// Implements <see cref="IReadRawTagValues"/>.
    /// </summary>
    internal class ReadRawTagValuesImpl : ProxyAdapterFeature, IReadRawTagValues {

        /// <summary>
        /// Creates a new <see cref="ReadRawTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public ReadRawTagValuesImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<TagValueQueryResult> ReadRawTagValues(
            IAdapterCallContext context,
            ReadRawTagValuesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach (var item in client.TagValues.ReadRawTagValuesAsync(
                    AdapterId,
                    request,
                    cancellationToken
                ).ConfigureAwait(false)) {
                    yield return item;
                }
            }
        }

    }
}
