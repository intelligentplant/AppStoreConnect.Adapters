using System;
using System.Threading.Tasks;

using DataCore.Adapter.Grpc;

using Grpc.Core;

namespace DataCore.Adapter.AspNetCore.Grpc.Services {

    /// <summary>
    /// Implements <see cref="HeartbeatService.HeartbeatServiceBase"/>.
    /// </summary>
    public class HeartbeatServiceImpl : HeartbeatService.HeartbeatServiceBase {

        /// <summary>
        /// Raised when a heartbeat message is received from a client. The parameter is the 
        /// caller's peer information.
        /// </summary>
        internal static event Action<string> HeartbeatReceived;


        /// <inheritdoc/>
        public override Task<Pong> Heartbeat(Ping request, ServerCallContext context) {
            var connectionId = string.IsNullOrWhiteSpace(request.SessionId)
                ? context.Peer
                : string.Concat(context.Peer, "-", request.SessionId);
            HeartbeatReceived?.Invoke(connectionId);
            return Task.FromResult(new Pong());
        }

    }
}
