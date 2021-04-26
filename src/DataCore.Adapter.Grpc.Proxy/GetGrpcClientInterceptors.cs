using System.Collections.Generic;

using Grpc.Core.Interceptors;

namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Gets interceptors to add to a gRPC client.
    /// </summary>
    /// <returns>
    ///   The interceptors.
    /// </returns>
    public delegate IEnumerable<Interceptor> GetGrpcClientInterceptors();

}
