using System.Linq;

namespace Microsoft.AspNetCore.Routing {

    /// <summary>
    /// Extensions for <see cref="EndpointDataSource"/>.
    /// </summary>
    public static class AdapterGrpcEndpointDataSourceExtensions {

        /// <summary>
        /// Checks if the App Store Connect Adapter gRPC API routes are registered with the 
        /// <see cref="EndpointDataSource"/>.
        /// </summary>
        /// <param name="endpointDataSource">
        ///   The <see cref="EndpointDataSource"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the App Store Connect Adapter gRPC routes are registered, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsGrpcAdapterApiRegistered(this EndpointDataSource endpointDataSource) {
            return endpointDataSource.Endpoints.Any(x => x is RouteEndpoint endpoint && endpoint.RoutePattern.RawText != null && endpoint.RoutePattern.RawText.StartsWith("/datacore.adapter.") && endpoint.Metadata.OfType<Grpc.AspNetCore.Server.GrpcMethodMetadata>().Any());
        }

    }
}
