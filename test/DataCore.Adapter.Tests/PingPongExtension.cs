﻿using System;
using System.Threading;
using System.Threading.Channels;
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


        internal PingPongExtension(AdapterBase adapter) : this(adapter.BackgroundTaskService) { }


        internal PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
            BindInvoke<PingMessage, PongMessage>(Ping);
            BindStream<PingMessage, PongMessage>(Ping);
            BindDuplexStream<PingMessage, PongMessage>(Ping);

            BindInvoke(Greet);
        }


        public PongMessage Ping(IAdapterCallContext context, PingMessage message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            return new PongMessage() {
                CorrelationId = message.CorrelationId,
                UtcServerTime = DateTime.UtcNow
            };
        }


        public Task<ChannelReader<PongMessage>> Ping(
            IAdapterCallContext context,
            PingMessage message,
            CancellationToken cancellationToken
        ) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            var result = Channel.CreateUnbounded<PongMessage>();

            result.Writer.RunBackgroundOperation((ch, ct) => {
                result.Writer.TryWrite(new PongMessage() {
                    CorrelationId = message.CorrelationId,
                    UtcServerTime = DateTime.UtcNow
                });
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public Task<ChannelReader<PongMessage>> Ping(
            IAdapterCallContext context,
            ChannelReader<PingMessage> channel,
            CancellationToken cancellationToken
        ) {
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var result = Channel.CreateUnbounded<PongMessage>();

            result.Writer.RunBackgroundOperation(async (ch, ct) => {
                while (await channel.WaitToReadAsync(ct)) {
                    while (channel.TryRead(out var message)) {
                        if (message == null) {
                            continue;
                        }

                        result.Writer.TryWrite(new PongMessage() {
                            CorrelationId = message.CorrelationId,
                            UtcServerTime = DateTime.UtcNow
                        });
                    }
                }
            }, true, BackgroundTaskService, cancellationToken);

            return Task.FromResult(result.Reader);
        }


        public string Greet() {
            return "Hello, world!";
        }
    }


    internal class PingMessage {

        public Guid CorrelationId { get; set; }

        public DateTime UtcClientTime { get; set; }

    }


    internal class PongMessage {

        public Guid CorrelationId { get; set; }

        public DateTime UtcServerTime { get; set; }

    }


    internal static class HelloWorldConstants {

        public const string FeatureUri = WellKnownFeatures.Extensions.BaseUri + "unit-tests/hello-world/";

    }


    [ExtensionFeature(HelloWorldConstants.FeatureUri)]
    internal interface IHelloWorld : IAdapterExtensionFeature {

        string Greet();

    }

}
