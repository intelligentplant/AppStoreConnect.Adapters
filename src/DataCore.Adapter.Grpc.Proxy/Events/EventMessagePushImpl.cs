using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {

    /// <summary>
    /// <see cref="IEventMessagePush"/> implementation.
    /// </summary>
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        /// <summary>
        /// Creates a new <see cref="EventMessagePushImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public EventMessagePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async IAsyncEnumerable<Adapter.Events.EventMessage> Subscribe(
            IAdapterCallContext context, 
            CreateEventMessageSubscriptionRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<EventsService.EventsServiceClient>();

            var grpcRequest = new CreateEventPushChannelRequest() {
                AdapterId = AdapterId,
                SubscriptionType = request.SubscriptionType == EventMessageSubscriptionType.Active
                    ? EventSubscriptionType.Active
                    : EventSubscriptionType.Passive
            };

            if (request.Properties != null) {
                foreach (var item in request.Properties) {
                    grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken))
            using (var grpcChannel = client.CreateEventPushChannel(
               grpcRequest,
               GetCallOptions(context, ctSource.Token)
            )) {
                // Read event messages.
                while (await grpcChannel.ResponseStream.MoveNext(ctSource.Token).ConfigureAwait(false)) {
                    if (grpcChannel.ResponseStream.Current == null) {
                        continue;
                    }

                    yield return grpcChannel.ResponseStream.Current.ToAdapterEventMessage();
                }
            }
        }

    }
}
