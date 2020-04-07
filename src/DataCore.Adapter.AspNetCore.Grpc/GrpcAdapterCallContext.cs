using Grpc.Core;

namespace DataCore.Adapter.AspNetCore.Grpc {

    /// <summary>
    /// <see cref="IAdapterCallContext"/> implementation that uses a <see cref="ServerCallContext"/> to 
    /// provide context settings.
    /// </summary>
    public class GrpcAdapterCallContext : HttpAdapterCallContext {

        /// <summary>
        /// Creates a new <see cref="GrpcAdapterCallContext"/> object.
        /// </summary>
        /// <param name="serverCallContext">
        ///   The gRPC server call context.
        /// </param>
        public GrpcAdapterCallContext(ServerCallContext serverCallContext)
            : base(serverCallContext?.GetHttpContext()) { }

    }
}
