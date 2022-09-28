using System.Linq;

namespace Microsoft.AspNetCore.Routing {

    /// <summary>
    /// Extensions for <see cref="EndpointDataSource"/>.
    /// </summary>
    public static class AdapterSignalREndpointDataSourceExtensions {

        /// <summary>
        /// Checks if the App Store Connect Adapter SignalR API routes are registered with the 
        /// <see cref="EndpointDataSource"/>.
        /// </summary>
        /// <param name="endpointDataSource">
        ///   The <see cref="EndpointDataSource"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the App Store Connect Adapter SignalR routes are registered, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsSignalRAdapterApiRegistered(this EndpointDataSource endpointDataSource) {
            return endpointDataSource.Endpoints.Any(x => x is RouteEndpoint endpoint && endpoint.RoutePattern.RawText != null && endpoint.RoutePattern.RawText.StartsWith("/signalr/app-store-connect/") && endpoint.Metadata.OfType<SignalR.HubMetadata>().Any());
        }

    }
}
