using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
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
    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
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

            BindInvoke<IHelloWorld, string>(Greet);
        }


        [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingInvokeDescriptor))]
        public Task<PongMessage> PingInvoke(
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
        public async IAsyncEnumerable<PongMessage> PingStream(
            PingMessage ping,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (ping == null) {
                throw new ArgumentNullException(nameof(ping));
            }

            await Task.CompletedTask.ConfigureAwait(false);
            yield return new PongMessage() {
                CorrelationId = ping.CorrelationId,
                UtcServerTime = DateTime.UtcNow
            };
        }


        [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(GetPingDuplexStreamDescriptor))]
        public async IAsyncEnumerable<PongMessage> PingDuplexStream(
            IAsyncEnumerable<PingMessage> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            await foreach(var ping in channel.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                if (ping == null) {
                    continue;
                }

                yield return new PongMessage() {
                    CorrelationId = ping.CorrelationId,
                    UtcServerTime = DateTime.UtcNow
                };
            }
        }


        public PongMessage[] PingArray1D(PingMessage[] messages) {
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


        public PongMessage[,] PingArray2D(PingMessage[,] messages) {
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


        public string Greet() {
            return "Hello, world!";
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
                Description = "Returns a greeting when invoked"
            };
        }

    }


    [ExtensionFeatureDataType(typeof(PingPongExtension), "ping-message")]
    internal class PingMessage {

        [Required]
        public Guid CorrelationId { get; set; }

        [Required]
        public DateTime UtcClientTime { get; set; }

    }


    [ExtensionFeatureDataType(typeof(PingPongExtension), "pong-message")]
    internal class PongMessage {

        [Required]
        public Guid CorrelationId { get; set; }

        [Required]
        public DateTime UtcServerTime { get; set; }

    }


    internal static class HelloWorldConstants {

        public const string FeatureUri = WellKnownFeatures.Extensions.BaseUri + "unit-tests/hello-world/";

    }


    [ExtensionFeature(HelloWorldConstants.FeatureUri)]
    internal interface IHelloWorld : IAdapterExtensionFeature {

        [ExtensionFeatureOperation(typeof(PingPongExtension), nameof(PingPongExtension.GetGreetDescriptor))]
        string Greet();

    }

}
