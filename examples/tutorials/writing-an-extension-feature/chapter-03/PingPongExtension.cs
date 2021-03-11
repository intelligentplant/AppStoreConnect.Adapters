using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Common;
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

        public PingPongExtension(IBackgroundTaskService backgroundTaskService, params IObjectEncoder[] encoders) : base(backgroundTaskService, encoders) {
            BindInvoke<PingPongExtension>(
                // Handler
                (ctx, req, ct) => {
                    var pingMessage = Decode<PingMessage>(req.Arguments.FirstOrDefault());
                    var pongMessage = Ping(pingMessage);
                    return Task.FromResult(new InvocationResponse() {
                        Results = new[] { Encode(pongMessage) }
                    });
                },
                // Operation name
                nameof(Ping),
                // Description
                "Responds to a ping message with a pong message",
                // Input parameter descriptions
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                // Output parameter descriptions
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 1,
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The pong message"
                    }
                }
            );

            BindStream<PingPongExtension>(
                // Handler
                (ctx, req, ct) => {
                    // The handler delegate requires that we return a Task<ChannelReader<InvocationResponse>>.
                    var outChannel = Channel.CreateUnbounded<InvocationResponse>();

                    var pingMessage = Decode<PingMessage>(req.Arguments.FirstOrDefault());

                    // Start a background task that will write results into our channel whenever 
                    // we receive a new input.
                    outChannel.Writer.RunBackgroundOperation(async (ch, ct2) => {
                        while (!ct2.IsCancellationRequested) {
                            // Every second, we will return a new PongMessage
                            await Task.Delay(1000, ct2);

                            var pongMessage = Ping(pingMessage);
                            await ch.WriteAsync(new InvocationResponse() {
                                Results = new[] { Encode(pongMessage) }
                            }, ct2);
                        }
                    }, true, backgroundTaskService, ct);

                    // Return the reader portion of the channel.
                    return Task.FromResult(outChannel.Reader);
                },
                // Operation name
                nameof(Ping),
                // Description
                "Responds to a ping message with a stream of pong messages",
                // Input parameter descriptions
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                // Output parameter descriptions
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 1,
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The pong message"
                    }
                }
            );
        }


        public PongMessage Ping(PingMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            return new PongMessage() {
                CorrelationId = message.CorrelationId
            };
        }

    }
}
