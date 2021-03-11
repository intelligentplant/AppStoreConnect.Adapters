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
    public class PingPongExtension : AdapterExtensionFeature, ITemperatureConverter {

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

            BindDuplexStream<PingPongExtension>(
                // Handler
                (ctx, req, inChannel, ct) => {
                    // The handler delegate requires that we return a Task<ChannelReader<InvocationResponse>>.
                    var outChannel = Channel.CreateUnbounded<InvocationResponse>();

                    // Start a background task that will write results into our channel whenever 
                    // we receive a new input.
                    outChannel.Writer.RunBackgroundOperation(async (ch, ct2) => {
                        // First, we process the ping message in the original request.
                        var pingMessage = Decode<PingMessage>(req.Arguments.FirstOrDefault());
                        var pongMessage = Ping(pingMessage);
                        await ch.WriteAsync(new InvocationResponse() {
                            Results = new[] { Encode(pongMessage) }
                        }, ct2);

                        // Now, we process the additional ping messages that are streamed into the 
                        // inChannel.
                        await foreach (var update in inChannel.ReadAllAsync(ct2)) {
                            pingMessage = Decode<PingMessage>(update.Arguments.FirstOrDefault());
                            pongMessage = Ping(pingMessage);
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
                "Responds to each ping message in the incoming stream with a pong message",
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

            // ITemperatureConverter bindings

            BindInvoke<ITemperatureConverter>(
                (ctx, req, ct) => {
                    var inTemp = Decode<double>(req.Arguments.FirstOrDefault());
                    var outTemp = CtoF(inTemp);
                    return Task.FromResult(new InvocationResponse() { 
                        Results = new[] { Encode(outTemp) }
                    });
                },
                nameof(CtoF),
                "Converts a temperature in Celsius to Fahrenheit",
                new [] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        TypeId = TypeLibrary.GetTypeId<double>(),
                        Description = "The temperature in Celsius."
                    }
                },
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        TypeId = TypeLibrary.GetTypeId<double>(),
                        Description = "The temperature in Fahrenheit."
                    }
                }
            );

            BindInvoke<ITemperatureConverter>(
                (ctx, req, ct) => {
                    var inTemp = Decode<double>(req.Arguments.FirstOrDefault());
                    var outTemp = FtoC(inTemp);
                    return Task.FromResult(new InvocationResponse() {
                        Results = new[] { Encode(outTemp) }
                    });
                },
                nameof(FtoC),
                "Converts a temperature in Fahrenheit to Celsius",
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        TypeId = TypeLibrary.GetTypeId<double>(),
                        Description = "The temperature in Fahrenheit."
                    }
                },
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        TypeId = TypeLibrary.GetTypeId<double>(),
                        Description = "The temperature in Celsius."
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


        public double CtoF(double degC) {
            return (degC * 1.8) + 32;
        }

        public double FtoC(double degF) {
            return (degF - 32) / 1.8;
        }
    }
}
