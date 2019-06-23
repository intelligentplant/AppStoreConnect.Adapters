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
    internal class ReadSnapshotTagValuesImpl : ProxyAdapterFeature, IReadSnapshotTagValues {

        public ReadSnapshotTagValuesImpl(SignalRAdapterProxy proxy) : base (proxy) { }


        public ChannelReader<TagValueQueryResult> ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                var connection = await this.GetTagValuesHubConnection(ct).ConfigureAwait(false);
                var hubChannel = await connection.StreamAsChannelAsync<TagValueQueryResult>(
                    "ReadSnapshotTagValues", 
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
