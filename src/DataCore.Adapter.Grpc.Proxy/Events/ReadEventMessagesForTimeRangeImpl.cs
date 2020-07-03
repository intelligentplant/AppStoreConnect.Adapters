using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {

    /// <summary>
    /// <see cref="IReadEventMessagesForTimeRange"/> implementation.
    /// </summary>
    internal class ReadEventMessagesForTimeRangeImpl : ProxyAdapterFeature, IReadEventMessagesForTimeRange {

        /// <summary>
        /// Creates a new <see cref="ReadEventMessagesForTimeRangeImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public ReadEventMessagesForTimeRangeImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public Task<ChannelReader<Adapter.Events.EventMessage>> ReadEventMessagesForTimeRange(IAdapterCallContext context, Adapter.Events.ReadEventMessagesForTimeRangeRequest request, CancellationToken cancellationToken) {
            var client = CreateClient<EventsService.EventsServiceClient>();
            var grpcRequest = new GetEventMessagesForTimeRangeRequest() {
                AdapterId = AdapterId,
                UtcStartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcStartTime),
                UtcEndTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(request.UtcEndTime),
                Direction = request.Direction.ToGrpcEventReadDirection(),
                PageSize = request.PageSize,
                Page = request.Page
            };
            if (request.Topics != null) {
                foreach (var item in request.Topics) {
                    grpcRequest.Topics.Add(item ?? string.Empty);
                }
            }
            if (request.Properties != null) {
                foreach (var prop in request.Properties) {
                    grpcRequest.Properties.Add(prop.Key, prop.Value ?? string.Empty);
                }
            }
            var grpcResponse = client.GetEventMessagesForTimeRange(grpcRequest, GetCallOptions(context, cancellationToken));

            var result = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.EventMessage>(-1);

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                try {
                    while (await grpcResponse.ResponseStream.MoveNext(ct).ConfigureAwait(false)) {
                        if (grpcResponse.ResponseStream.Current == null) {
                            continue;
                        }
                        await ch.WriteAsync(grpcResponse.ResponseStream.Current.ToAdapterEventMessage(), ct).ConfigureAwait(false);
                    }
                }
                finally {
                    grpcResponse.Dispose();
                }
            }, true, TaskScheduler, cancellationToken);

            return Task.FromResult(result.Reader);
        }
    }
}
