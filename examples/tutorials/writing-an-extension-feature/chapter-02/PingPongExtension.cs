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

    }
}
