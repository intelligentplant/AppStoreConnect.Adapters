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
            BindInvoke<PingPongExtension, PingMessage, PongMessage>(
                Ping,
                description: "Responds to a ping message with a pong message",
                inputParameters: new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                outputParameters: new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The pong message"
                    }
                }
            );

            BindStream<PingPongExtension, PingMessage, PongMessage>(
                Ping,
                description: "Responds to a ping message with a stream of pong messages",
                inputParameters: new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                outputParameters: new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The pong message"
                    }
                }
            );

            BindDuplexStream<PingPongExtension, PingMessage, PongMessage>(
                Ping,
                description: "Responds to each ping message in the incoming stream with a pong message",
                inputParameters: new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                outputParameters: new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The pong message"
                    }
                }
            );
        }


        public PongMessage Ping(IAdapterCallContext context, PingMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            return new PongMessage() {
                CorrelationId = message.CorrelationId
            };
        }


        public Task<ChannelReader<PongMessage>> Ping(IAdapterCallContext context, PingMessage message, CancellationToken cancellationToken) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            var result = Channel.CreateUnbounded<PongMessage>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (!ct.IsCancellationRequested) {
                    // Every second, we will return a new PongMessage
                    await Task.Delay(1000, ct);

                    var pongMessage = Ping(context, message);
                    await ch.WriteAsync(pongMessage, ct);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public Task<ChannelReader<PongMessage>> Ping(IAdapterCallContext context, ChannelReader<PingMessage> messages, CancellationToken cancellationToken) {
            if (messages == null) {
                throw new ArgumentNullException(nameof(messages));
            }

            var result = Channel.CreateUnbounded<PongMessage>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                await foreach (var pingMessage in messages.ReadAllAsync(ct)) {
                    if (pingMessage == null) {
                        continue;
                    }
                    var pongMessage = Ping(context, pingMessage);
                    await ch.WriteAsync(pongMessage, ct);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }

    }
}
