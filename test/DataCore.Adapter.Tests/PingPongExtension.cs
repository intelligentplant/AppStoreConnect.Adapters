using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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


        internal PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
            BindInvoke<PingPongExtension, PingMessage, PongMessage>(PingInvoke);
            BindStream<PingPongExtension, PingMessage, PongMessage>(PingStream);
            BindDuplexStream<PingPongExtension, PingMessage, PongMessage>(PingDuplexStream);

            BindInvoke<PingPongExtension, PingMessage[], PongMessage[]>(PingArray1D);

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


        public string Greet() {
            return "Hello, world!";
        }


        internal static ExtensionFeatureOperationDescriptorPartial GetPingInvokeDescriptor() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Name = "Ping",
                Description = "Returns a pong message that matches the correlation ID of the specified ping message"
            };
        }


        internal static ExtensionFeatureOperationDescriptorPartial GetPingStreamDescriptor() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Name = "Ping",
                Description = "Returns a pong message every second that matches the correlation ID of the specified ping message"
            };
        }


        internal static ExtensionFeatureOperationDescriptorPartial GetPingDuplexStreamDescriptor() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Name = "Ping",
                Description = "Returns a pong message every time a ping message is received"
            };
        }


        internal static ExtensionFeatureOperationDescriptorPartial GetGreetDescriptor() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Description = "Returns a greeting when invoked"
            };
        }

    }


    [Description("A ping message received by the ping pong service.")]
    internal class PingMessage {

        public Guid CorrelationId { get; set; }

        public DateTime UtcClientTime { get; set; }

    }


    [Description("A pong message generated from a corresponding ping message.")]
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
        string Greet();

    }

}
