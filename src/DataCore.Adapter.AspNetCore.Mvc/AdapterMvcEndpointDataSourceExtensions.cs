using System.Linq;

namespace Microsoft.AspNetCore.Routing {

    /// <summary>
    /// Extensions for <see cref="EndpointDataSource"/>.
    /// </summary>
    public static class AdapterMvcEndpointDataSourceExtensions {

        /// <summary>
        /// Checks if the App Store Connect Adapter MVC API routes are registered with the 
        /// <see cref="EndpointDataSource"/>.
        /// </summary>
        /// <param name="endpointDataSource">
        ///   The <see cref="EndpointDataSource"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the App Store Connect Adapter MVC routes are registered, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsMvcAdapterApiRegistered(this EndpointDataSource endpointDataSource) {
            return endpointDataSource.Endpoints.Any(x => x is RouteEndpoint endpoint && endpoint.RoutePattern.RawText != null && endpoint.RoutePattern.RawText.StartsWith("api/app-store-connect/") && endpoint.Metadata.OfType<Mvc.RouteAttribute>().Any());
        }

    }
}
