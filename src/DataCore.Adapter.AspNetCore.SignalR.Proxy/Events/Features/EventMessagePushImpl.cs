using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using DataCore.Adapter.Events.Features;
using DataCore.Adapter.Events.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Events.Features {
    internal class EventMessagePushImpl : ProxyAdapterFeature, IEventMessagePush {

        public EventMessagePushImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        public async Task<IEventMessageSubscription> Subscribe(IAdapterCallContext context, bool active, CancellationToken cancellationToken) {
            var result = new EventMessageSubscription(
                AdapterId,
                await GetHubConnection(cancellationToken).ConfigureAwait(false),
                active
            );
            result.Start();
            return result;
        }


        private class EventMessageSubscription : IEventMessageSubscription {

            private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

            private readonly string _adapterId;

            private readonly HubConnection _hubConnection;

            private readonly Channel<EventMessage> _channel = ChannelExtensions.CreateEventMessageChannel<EventMessage>();

            private readonly bool _activeSubscription;

            public ChannelReader<EventMessage> Reader { get { return _channel; } }


            public EventMessageSubscription(string adapterId, HubConnection hubConnection, bool activeSubscription) {
                _adapterId = adapterId;
                _hubConnection = hubConnection;
                _activeSubscription = activeSubscription;
            }


            public void Start() {
                _channel.Writer.RunBackgroundOperation(async (ch, ct) => {
                    var hubChannel = await _hubConnection.StreamAsChannelAsync<EventMessage>(
                        "CreateEventMessageChannel",
                        _adapterId,
                        _activeSubscription,
                        ct
                    ).ConfigureAwait(false);

                    await hubChannel.Forward(ch, ct).ConfigureAwait(false);
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
