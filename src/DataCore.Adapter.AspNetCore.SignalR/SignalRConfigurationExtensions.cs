using System;
using System.Collections.Generic;
using System.Text;
using DataCore.Adapter.AspNetCore.Hubs;

#if NETCOREAPP3_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
#else
using Microsoft.AspNetCore.SignalR;
#endif

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class SignalRConfigurationExtensions {

        /// <summary>
        /// SignalR Hub route.
        /// </summary>
        public const string HubRoute = "/signalr/data-core/v1.0";


        /// <summary>
        /// Maps adapter hub endpoints.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <returns>
        ///   The endpoint route builder.
        /// </returns>
#if NETCOREAPP3_0
        public static IEndpointRouteBuilder MapDataCoreAdapterHubs(this IEndpointRouteBuilder endpoints) {
#else
        public static HubRouteBuilder MapDataCoreAdapterHubs(this HubRouteBuilder endpoints) {
#endif
            endpoints.MapHub<AdapterHub>(HubRoute);
            return endpoints;
        }

    }
}
