using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using DataCore.Adapter.Events.Features;

namespace DataCore.Adapter.Grpc.Proxy.Events.Features {
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        public EventMessagePushImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        public Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, bool active, CancellationToken cancellationToken) {
            var result = new EventMessageSubscription(AdapterId, CreateClient<EventsService.EventsServiceClient>(), active);
            result.Start();
            return Task.FromResult<IEventMessageSubscription>(result);
        }


        private class EventMessageSubscription : IEventMessageSubscription {

            private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

            private readonly string _adapterId;

            private readonly EventsService.EventsServiceClient _client;

            private readonly System.Threading.Channels.Channel<Adapter.Events.Models.EventMessage> _channel = ChannelExtensions.CreateEventMessageChannel<Adapter.Events.Models.EventMessage>();

            private readonly bool _activeSubscription;

            public System.Threading.Channels.ChannelReader<Adapter.Events.Models.EventMessage> Reader { get { return _channel; } }


            public EventMessageSubscription(string adapterId, EventsService.EventsServiceClient client, bool activeSubscription) {
                _adapterId = adapterId;
                _client = client;
                _activeSubscription = activeSubscription;
            }


            public void Start() {
                _channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var grpcResponse = _client.CreateEventPushChannel(new CreateEventPushChannelRequest() {
                        AdapterId = _adapterId,
                        Active = _activeSubscription
                    }, cancellationToken: ct);

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
                }, false, _shutdownTokenSource.Token);
            }


            public void Dispose() {
                _shutdownTokenSource.Cancel();
                _shutdownTokenSource.Dispose();
                _channel.Writer.TryComplete();
            }
        }
    }
}
