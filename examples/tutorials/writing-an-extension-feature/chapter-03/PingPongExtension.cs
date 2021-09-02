using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
            BindInvoke<PingPongExtension, PingMessage, PongMessage>(
                Ping,
                description: "Responds to a ping message with a pong message"
            );

            BindStream<PingPongExtension, PingMessage, PongMessage>(
                Ping,
                description: "Responds to a ping message with a stream of pong messages"
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


        public async IAsyncEnumerable<PongMessage> Ping(
            IAdapterCallContext context, 
            PingMessage message, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            while (!cancellationToken.IsCancellationRequested) {
                // Every second, we will return a new PongMessage
                await Task.Delay(1000, cancellationToken);

                yield return Ping(context, message);
            }
        }

    }
}
