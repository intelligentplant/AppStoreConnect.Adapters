using System;
using System.Threading;
using System.Threading.Channels;
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
        public async Task<ChannelReader<TagValueQueryResult>> ReadRawTagValues(IAdapterCallContext context, ReadRawTagValuesRequest request, CancellationToken cancellationToken) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            SignalRAdapterProxy.ValidateObject(request); 
            
            var client = GetClient();
            var hubChannel = await client.TagValues.ReadRawTagValuesAsync(
                AdapterId, 
                request, 
                cancellationToken
            ).ConfigureAwait(false);

            var result = ChannelExtensions.CreateTagValueChannel<TagValueQueryResult>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await hubChannel.Forward(ch, ct).ConfigureAwait(false);
            }, true, TaskScheduler, cancellationToken);

            return result;
        }

    }
}
