using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using DataCore.Adapter.RealTimeData.Features;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.RealTimeData.Features {
    internal class ReadRawTagValuesImpl : ProxyAdapterFeature, IReadRawTagValues {

        public ReadRawTagValuesImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        public ChannelReader<TagValueQueryResult> ReadRawTagValues(IAdapterCallContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateBoundedTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await this.GetTagValuesHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<TagValueQueryResult>(
                    "ReadRawTagValues",
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
