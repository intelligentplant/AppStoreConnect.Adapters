using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Delegate for requesting per-call credentials for a gRPC request.
    /// </summary>
    /// <param name="context">
    ///   The adapter call context.
    /// </param>
    /// <returns>
    ///   The gRPC call credentials. Return <see langword="null"/> to use only the channel-level 
    ///   credentials.
    /// </returns>
    public delegate Task<GrpcAdapterProxyCallCredentials> GetGrpcCallCredentials(IAdapterCallContext context);

}
