using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Features;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class EventsServiceImpl : EventsService.EventsServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;

        private static readonly ConcurrentDictionary<string, Events.IEventMessageSubscription> s_subscriptions = new ConcurrentDictionary<string, Events.IEventMessageSubscription>();


        public EventsServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        public override async Task CreateEventPushChannel(CreateEventPushChannelRequest request, IServerStreamWriter<EventMessagePush> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IEventMessagePush>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var key = $"{_adapterCallContext.ConnectionId}:{nameof(EventsServiceImpl)}:{adapter.Adapter.Descriptor.Id}:{request.Active}".ToUpperInvariant();
            if (s_subscriptions.TryGetValue(key, out var _)) {
                throw new RpcException(new Status(StatusCode.AlreadyExists, string.Format(Resources.Error_DuplicateEventSubscriptionAlreadyExists, adapterId)));
            }

            using (var subscription = adapter.Feature.Subscribe(_adapterCallContext, request.Active)) {
                try {
                    s_subscriptions[key] = subscription;
                    while (!context.CancellationToken.IsCancellationRequested) {
                        try {
                            var msg = await subscription.Reader.ReadAsync(context.CancellationToken).ConfigureAwait(false);
                            await responseStream.WriteAsync(new EventMessagePush() {
                                EventMessage = msg.ToGrpcEventMessage()
                            }).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) {
                            // Do nothing
                        }
                    }
                }
                finally {
                    s_subscriptions.TryRemove(key, out var _);
                }
            }
        }

    }
}
