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
                new [] {
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
