
using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Client.Authentication {

    /// <summary>
    /// Interface that can be used to define call credentials to be added to a gRPC call.
    /// </summary>
    public interface IClientCallCredentials {

        /// <summary>
        /// Gets a gRPC metadata entry representing the credentials.
        /// </summary>
        /// <returns>
        ///   A gRPC metadata entry representing the credentials.
        /// </returns>
        GrpcCore.Metadata.Entry GetMetadataEntry();

    }
}
