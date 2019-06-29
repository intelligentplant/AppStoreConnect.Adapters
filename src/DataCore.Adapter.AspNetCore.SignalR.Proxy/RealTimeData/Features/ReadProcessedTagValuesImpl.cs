using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {
    internal class ReadProcessedTagValuesImpl : ProxyAdapterFeature, IReadProcessedTagValues {

        public ReadProcessedTagValuesImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        public async Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctions(IAdapterCallContext context, CancellationToken cancellationToken) {
            var connection = await GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<IEnumerable<DataFunctionDescriptor>>(
                "GetSupportedDataFunctions",
                AdapterId,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public ChannelReader<ProcessedTagValueQueryResult> ReadProcessedTagValues(IAdapterCallContext context, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<ProcessedTagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await GetHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<ProcessedTagValueQueryResult>(
                    "ReadProcessedTagValues",
                    AdapterId,
                    request,
                    cancellationToken
                ).ConfigureAwait(false);
                await hubChannel.Forward(ch, cancellationToken).ConfigureAwait(false);
            }, true, cancellationToken);

            return result;
        }

        
    }
}
