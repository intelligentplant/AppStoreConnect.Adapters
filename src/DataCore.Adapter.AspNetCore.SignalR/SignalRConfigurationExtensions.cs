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
        /// Prefix for all SignalR hub routes.
        /// </summary>
        private const string HubRoutePrefix = "/signalr/data-core/v1.0";


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
            endpoints.MapHub<AssetModelBrowserHub>($"{HubRoutePrefix}/asset-model-browser");
            endpoints.MapHub<EventsHub>($"{HubRoutePrefix}/events");
            endpoints.MapHub<TagAnnotationsHub>($"{HubRoutePrefix}/tag-annotations");
            endpoints.MapHub<TagSearchHub>($"{HubRoutePrefix}/tag-search");
            endpoints.MapHub<TagValuesHub>($"{HubRoutePrefix}/tag-values");
            return endpoints;
        }

    }
}
