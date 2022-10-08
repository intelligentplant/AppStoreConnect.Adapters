using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Http.Proxy.RealTimeData {
    /// <summary>
    /// Implements <see cref="IWriteSnapshotTagValues"/>.
    /// </summary>
    internal class WriteSnapshotTagValuesImpl : ProxyAdapterFeature, IWriteSnapshotTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteSnapshotTagValuesImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public WriteSnapshotTagValuesImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc />
        public async IAsyncEnumerable<WriteTagValueResult> WriteSnapshotTagValues(
            IAdapterCallContext context,
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request, channel);

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                if (Proxy.CanUseSignalR) {
                    var client = GetSignalRClient(context);
                    await client.StreamStartedAsync().ConfigureAwait(false);
                    try { 
                        await foreach (var item in client.Client.TagValues.WriteSnapshotTagValuesAsync(AdapterId, request, channel, ctSource.Token).ConfigureAwait(false)) {
                            if (item == null) {
                                continue;
                            }
                            yield return item;
                        }
                    }
                    finally {
                        await client.StreamCompletedAsync().ConfigureAwait(false);
                    }
                }
                else {
                    var client = GetClient();

                    const int maxItems = 5000;
                    var items = (await channel.ToEnumerable(maxItems, ctSource.Token).ConfigureAwait(false)).ToArray();
                    if (items.Length >= maxItems) {
                        Logger.LogInformation("The maximum number of items that can be written to the remote adapter ({MaxItems}) was read from the channel. Any remaining items will be ignored.", maxItems);
                    }

                    var req = new WriteTagValuesRequestExtended() {
                        Values = items,
                        Properties = request.Properties
                    };

                    await foreach (var item in client.TagValues.WriteSnapshotValuesAsync(AdapterId, req, context?.ToRequestMetadata(), ctSource.Token).ConfigureAwait(false)) {
                        if (item == null) {
                            continue;
                        }
                        yield return item;
                    }

                }
            }
        }
    }
}
