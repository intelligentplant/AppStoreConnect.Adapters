using DataCore.Adapter.AspNetCore.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class AspNetCoreConfigurationExtensions {

        /// <summary>
        /// Adds the adapter API controllers to the MVC registration.
        /// </summary>
        /// <param name="builder">
        ///   The MVC builder.
        /// </param>
        /// <returns>
        ///   The MVC builder.
        /// </returns>
        public static IMvcBuilder AddDataCoreAdapterMvc(this IMvcBuilder builder) {
            builder.AddApplicationPart(typeof(AspNetCoreConfigurationExtensions).Assembly);

            return builder;
        }


        /// <summary>
        /// Adds adapter hubs to the SignalR registration.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <returns>
        ///   The endpoint route builder.
        /// </returns>
        public static IEndpointRouteBuilder MapDataCoreAdapterHubs(this IEndpointRouteBuilder endpoints) {
            const string routePrefix = "/signalr/data-core/v1.0";
            endpoints.MapHub<AssetModelBrowserHub>($"{routePrefix}/asset-model-browser");
            endpoints.MapHub<EventsHub>($"{routePrefix}/events");
            endpoints.MapHub<TagAnnotationsHub>($"{routePrefix}/tag-annotations");
            endpoints.MapHub<TagSearchHub>($"{routePrefix}/tag-search");
            endpoints.MapHub<TagValuesHub>($"{routePrefix}/tag-values");
            return endpoints;
        }

    }

}
