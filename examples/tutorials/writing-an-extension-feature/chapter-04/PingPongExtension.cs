using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;

namespace MyAdapter {

    [ExtensionFeature(
        ExtensionUri,
        Name = "Ping Pong",
        Description = "Example extension feature."
    )]
    public class PingPongExtension : AdapterExtensionFeature {

        public const string ExtensionUri = "tutorial/ping-pong/";

        public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
            BindInvoke<PingMessage, PongMessage>(Ping);
            BindStream<PingMessage, PongMessage>(Ping);
            BindDuplexStream<PingMessage, PongMessage>(Ping);
        }


        [ExtensionFeatureOperation(
            Description = "Responds to a ping message with a pong message",
            InputParameterDescription = "The ping message",
            OutputParameterDescription = "The pong message"
        )]
        public PongMessage Ping(PingMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            return new PongMessage() {
                CorrelationId = message.CorrelationId
            };
        }


        [ExtensionFeatureOperation(
            Description = "Responds to a ping message with a pong message every second until the call is cancelled",
            InputParameterDescription = "The ping message",
            OutputParameterDescription = "The pong message"
        )]
        public ChannelReader<PongMessage> Ping(PingMessage message, CancellationToken cancellationToken) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            var result = Channel.CreateUnbounded<PongMessage>();
            result.Writer.RunBackgroundOperation(async (ch, ct) => { 
                while (!ct.IsCancellationRequested) {
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    ch.TryWrite(new PongMessage() {
                        CorrelationId = message.CorrelationId
                    });
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }


        [ExtensionFeatureOperation(
            Description = "Responds to each ping message in an incoming stream with a pong message",
            InputParameterDescription = "The ping message",
            OutputParameterDescription = "The pong message"
        )]
        public ChannelReader<PongMessage> Ping(ChannelReader<PingMessage> messages, CancellationToken cancellationToken) {
            if (messages == null) {
                throw new ArgumentNullException(nameof(messages));
            }

            var result = Channel.CreateUnbounded<PongMessage>();
            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (await messages.WaitToReadAsync(ct).ConfigureAwait(false)) {
                    while (messages.TryRead(out var message)) {
                        if (message == null) {
                            continue;
                        }

                        ch.TryWrite(new PongMessage() {
                            CorrelationId = message.CorrelationId
                        });
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return result.Reader;
        }

    }
}
