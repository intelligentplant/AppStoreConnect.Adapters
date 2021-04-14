using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {
    /// <summary>
    /// Implements <see cref="IWriteHistoricalTagValues"/>.
    /// </summary>
    internal class WriteHistoricalTagValuesImpl : ProxyAdapterFeature, IWriteHistoricalTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteHistoricalTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public WriteHistoricalTagValuesImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<WriteTagValueResult> WriteHistoricalTagValues(
            IAdapterCallContext context, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            var client = GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                const int maxItems = 5000;
                var items = (await channel.ToEnumerable(maxItems, ctSource.Token).ConfigureAwait(false)).ToArray();
                if (items.Length >= maxItems) {
                    Logger.LogInformation("The maximum number of items that can be written to the remote adapter ({MaxItems}) was read from the channel. Any remaining items will be ignored.", maxItems);
                }

                var req = new WriteTagValuesRequestExtended() {
                    Values = items,
                    Properties = request.Properties
                };

                var clientResponse = await client.TagValues.WriteHistoricalValuesAsync(AdapterId, req, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false);
                foreach (var item in clientResponse) {
                    yield return item;
                }
            }
        }
    }
}
