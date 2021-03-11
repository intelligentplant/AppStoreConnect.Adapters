using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Tests {

    /// <summary>
    /// Extension feature implementation used in unit tests.
    /// </summary>
    /// <remarks>
    ///   Note that this class implements two extension features:
    /// 
    ///   * The class itself is an extension feature (since it inherits from <see cref="AdapterExtensionFeature"/> 
    ///     and is annotated with <see cref="ExtensionFeatureAttribute"/>).
    ///   * The class also implements the <see cref="IHelloWorld"/> extension defined in a separate interface.
    /// 
    /// </remarks>
    [ExtensionFeature(
        FeatureUri,
        Name = "Ping Pong",
        Description = "Responds to every ping message with a pong message"
    )]
    internal class PingPongExtension : AdapterExtensionFeature, IHelloWorld {

        public const string FeatureUri = WellKnownFeatures.Extensions.BaseUri + RelativeFeatureUri;

        public const string RelativeFeatureUri = "unit-tests/ping-pong/";


        internal PingPongExtension(IBackgroundTaskService backgroundTaskService, IEnumerable<IObjectEncoder> encoders) : base(backgroundTaskService, encoders) {
            BindInvoke<PingPongExtension>( 
                PingInvoke,
                "Ping",
                "Returns a pong message that matches the correlation ID of the specified ping message",
                new [] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                new [] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The resulting pong message"
                    }
                }
            );

            BindStream<PingPongExtension>(
                PingStream,
                "Ping",
                "Streams a pong message that matches the correlation ID of the specified ping message",
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The resulting pong message"
                    }
                }
            );

            BindDuplexStream<PingPongExtension>(
                PingDuplexStream,
                "Ping",
                "Streams pong messages that match the correlation IDs of the input stream ping messages",
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The resulting pong message"
                    }
                }
            );

            BindInvoke<IHelloWorld>(
                Greet,
                "Greet",
                "Returns a greeting.",
                null,
                new [] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        TypeId = TypeLibrary.GetTypeId<string>(),
                        Description = "The greeting"
                    }
                }
            );
        }


        public Task<InvocationResponse> PingInvoke(
            IAdapterCallContext context, 
            InvocationRequest message, 
            CancellationToken cancellationToken
        ) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            var ping = Decode<PingMessage>(message.Arguments.FirstOrDefault());
            if (ping == null) {
                return Task.FromResult(new InvocationResponse());
            }

            return Task.FromResult(new InvocationResponse() { 
                Results = new [] {
                    Encode(new PongMessage() {
                        CorrelationId = ping.CorrelationId,
                        UtcServerTime = DateTime.UtcNow
                    })
                }
            });
        }


        public Task<ChannelReader<InvocationResponse>> PingStream(
            IAdapterCallContext context,
            InvocationRequest message,
            CancellationToken cancellationToken
        ) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            var ping = Decode<PingMessage>(message.Arguments.FirstOrDefault());

            var result = Channel.CreateUnbounded<InvocationResponse>();

            result.Writer.RunBackgroundOperation((ch, ct) => {
                if (ping != null) {
                    result.Writer.TryWrite(new InvocationResponse() {
                        Results = new [] {
                            Encode(new PongMessage() {
                                CorrelationId = ping.CorrelationId,
                                UtcServerTime = DateTime.UtcNow
                            })
                        }
                    });
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public Task<ChannelReader<InvocationResponse>> PingDuplexStream(
            IAdapterCallContext context,
            InvocationRequest message,
            ChannelReader<InvocationStreamItem> channel,
            CancellationToken cancellationToken
        ) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var ping = Decode<PingMessage>(message.Arguments.FirstOrDefault());

            var result = Channel.CreateUnbounded<InvocationResponse>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                if (ping != null) {
                    result.Writer.TryWrite(new InvocationResponse() {
                        Results = new [] {
                            Encode(new PongMessage() {
                                CorrelationId = ping.CorrelationId,
                                UtcServerTime = DateTime.UtcNow
                            })
                        }
                    });
                }

                while (await channel.WaitToReadAsync(ct)) {
                    while (channel.TryRead(out var message)) {
                        if (message == null) {
                            continue;
                        }

                        ping = Decode<PingMessage>(message.Arguments.FirstOrDefault());

                        if (ping != null) {
                            result.Writer.TryWrite(new InvocationResponse() {
                                Results = new [] {
                                    Encode(new PongMessage() {
                                        CorrelationId = ping.CorrelationId,
                                        UtcServerTime = DateTime.UtcNow
                                    })
                                }
                            });
                        }
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public Task<InvocationResponse> Greet(
            IAdapterCallContext context,
            InvocationRequest message,
            CancellationToken cancellationToken
        ) {
            return Task.FromResult(new InvocationResponse() { 
                Results = new [] {
                    Encode("Hello, world!")
                }
            });
        }
    }


    [DataTypeId(PingPongExtension.FeatureUri + "types/ping")]
    internal class PingMessage {

        public Guid CorrelationId { get; set; }

        public DateTime UtcClientTime { get; set; }

    }


    [DataTypeId(PingPongExtension.FeatureUri + "types/pong")]
    internal class PongMessage {

        public Guid CorrelationId { get; set; }

        public DateTime UtcServerTime { get; set; }

    }


    internal static class HelloWorldConstants {

        public const string FeatureUri = WellKnownFeatures.Extensions.BaseUri + "unit-tests/hello-world/";

    }


    [ExtensionFeature(HelloWorldConstants.FeatureUri)]
    internal interface IHelloWorld : IAdapterExtensionFeature {

        Task<InvocationResponse> Greet(IAdapterCallContext context, InvocationRequest message, CancellationToken cancellationToken);

    }

}
