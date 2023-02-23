namespace Microsoft.AspNetCore.Routing {

    /// <summary>
    /// Extensions for <see cref="EndpointDataSource"/>.
    /// </summary>
    public static class AdapterMinimalApiEndpointDataSourceExtensions {

        /// <summary>
        /// Checks if the App Store Connect Adapter Minimal API routes are registered with the 
        /// <see cref="EndpointDataSource"/>.
        /// </summary>
        /// <param name="endpointDataSource">
        ///   The <see cref="EndpointDataSource"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the App Store Connect Adapter Minimal API routes are registered, 
        ///   or <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsAdapterMinimalApiRegistered(this EndpointDataSource endpointDataSource) {
            return endpointDataSource.Endpoints.Any(x => x is RouteEndpoint endpoint && endpoint.RoutePattern.RawText != null && endpoint.RoutePattern.RawText.StartsWith("/api/app-store-connect/"));
        }

    }
}
