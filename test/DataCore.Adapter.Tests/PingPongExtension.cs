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
            BindInvoke<PingPongExtension, PingMessage, PongMessage>(PingInvoke);
            BindStream<PingPongExtension, PingMessage, PongMessage>(PingStream);
            BindDuplexStream<PingPongExtension, PingMessage, PongMessage>(PingDuplexStream);

            BindInvoke<PingPongExtension, PingMessage[], PongMessage[]>(PingArray1D);
            BindInvoke<PingPongExtension, PingMessage[,], PongMessage[,]>(PingArray2D);

            BindInvoke<IHelloWorld>(Greet);
        }


        [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingInvokeDescriptor))]
        public Task<PongMessage> PingInvoke(
            IAdapterCallContext context, 
            PingMessage ping, 
            CancellationToken cancellationToken
        ) {
            if (ping == null) {
                throw new ArgumentNullException(nameof(ping));
            }

            return Task.FromResult(new PongMessage() {
                CorrelationId = ping.CorrelationId,
                UtcServerTime = DateTime.UtcNow
            });
        }


        [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingStreamDescriptor))]
        public Task<ChannelReader<PongMessage>> PingStream(
            IAdapterCallContext context,
            PingMessage ping,
            CancellationToken cancellationToken
        ) {
            if (ping == null) {
                throw new ArgumentNullException(nameof(ping));
            }

            var result = Channel.CreateUnbounded<PongMessage>();

            result.Writer.RunBackgroundOperation((ch, ct) => {
                ch.TryWrite(new PongMessage() {
                    CorrelationId = ping.CorrelationId,
                    UtcServerTime = DateTime.UtcNow
                });
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingDuplexStreamDescriptor))]
        public Task<ChannelReader<PongMessage>> PingDuplexStream(
            IAdapterCallContext context,
            ChannelReader<PingMessage> channel,
            CancellationToken cancellationToken
        ) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var result = Channel.CreateUnbounded<PongMessage>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (!ct.IsCancellationRequested) {
                    var ping = await channel.ReadAsync(ct).ConfigureAwait(false);
                    if (ping == null) {
                        continue;
                    }

                    await ch.WriteAsync(new PongMessage() {
                        CorrelationId = ping.CorrelationId,
                        UtcServerTime = DateTime.UtcNow
                    }, ct).ConfigureAwait(false);
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public PongMessage[] PingArray1D(IAdapterCallContext context, PingMessage[] messages) {
            var result = new PongMessage[messages.Length];

            for (var i = 0; i < messages.Length; i++) {
                var ping = messages[i];
                var pong = new PongMessage() {
                    CorrelationId = ping.CorrelationId,
                    UtcServerTime = DateTime.UtcNow
                };
                result[i] = pong;
            }

            return result;
        }


        public PongMessage[,] PingArray2D(IAdapterCallContext context, PingMessage[,] messages) {
            var len0 = messages.GetLength(0);
            var len1 = messages.GetLength(1);
            var result = new PongMessage[len0, len1];

            for (var i = 0; i < len0; i++) {
                for (var j = 0; j < len1; j++) {
                    var ping = messages[i, j];
                    var pong = new PongMessage() {
                        CorrelationId = ping.CorrelationId,
                        UtcServerTime = DateTime.UtcNow
                    };
                    result[i, j] = pong;
                }
            }

            return result;
        }


        public Task<InvocationResponse> Greet(
            IAdapterCallContext context,
            InvocationRequest message,
            CancellationToken cancellationToken
        ) {
            return Task.FromResult(new InvocationResponse() { 
                Results = new Variant[] {
                    this.ConvertToVariant("Hello, world!")
                }
            });
        }


        internal static ExtensionFeatureOperationDescriptorPartial GetPingInvokeDescriptor() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Name = "Ping",
                Description = "Returns a pong message that matches the correlation ID of the specified ping message",
                Inputs = new [] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                Outputs = new [] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The resulting pong message"
                    }
                }
            };
        }


        internal static ExtensionFeatureOperationDescriptorPartial GetPingStreamDescriptor() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Name = "Ping",
                Description = "Returns a pong message every second that matches the correlation ID of the specified ping message",
                Inputs = new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                Outputs = new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The resulting pong message"
                    }
                }
            };
        }


        internal static ExtensionFeatureOperationDescriptorPartial GetPingDuplexStreamDescriptor() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Name = "Ping",
                Description = "Returns a pong message every time a ping message is received",
                Inputs = new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                        Description = "The ping message"
                    }
                },
                Outputs = new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        VariantType = VariantType.ExtensionObject,
                        TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                        Description = "The resulting pong message"
                    }
                }
            };
        }


        internal static ExtensionFeatureOperationDescriptorPartial GetGreetDescriptor() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Name = "Greet",
                Description = "Returns a greeting when invoked"
            };
        }

    }


    [ExtensionFeatureDataType(typeof(PingPongExtension), "ping-message")]
    internal class PingMessage {

        public Guid CorrelationId { get; set; }

        public DateTime UtcClientTime { get; set; }

    }


    [ExtensionFeatureDataType(typeof(PingPongExtension), "pong-message")]
    internal class PongMessage {

        public Guid CorrelationId { get; set; }

        public DateTime UtcServerTime { get; set; }

    }


    internal static class HelloWorldConstants {

        public const string FeatureUri = WellKnownFeatures.Extensions.BaseUri + "unit-tests/hello-world/";

    }


    [ExtensionFeature(HelloWorldConstants.FeatureUri)]
    internal interface IHelloWorld : IAdapterExtensionFeature {

        [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(PingPongExtension.GetGreetDescriptor))]
        Task<InvocationResponse> Greet(IAdapterCallContext context, InvocationRequest message, CancellationToken cancellationToken);

    }

}
