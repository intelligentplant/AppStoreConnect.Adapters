using System;
using DataCore.Adapter.AspNetCore.Hubs;

#if NETSTANDARD2_0 == false
using DataCore.Adapter.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
#endif
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class SignalRConfigurationExtensions {

        /// <summary>
        /// SignalR Hub route.
        /// </summary>
        public const string HubRoute = "/signalr/app-store-connect/v1.0";

        /// <summary>
        /// Legacy SignalR hub route.
        /// </summary>
        [Obsolete("Use HubRoute instead.", false)]
        private const string LegacyHubRoute = "/signalr/data-core/v1.0";


        /// <summary>
        /// Configures services required for adapter SignalR.
        /// </summary>
        /// <param name="builder">
        ///   The SignalR server builder.
        /// </param>
        /// <returns>
        ///   The SignalR server builder.
        /// </returns>
        public static ISignalRServerBuilder AddDataCoreAdapterSignalR(this ISignalRServerBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

#if NETSTANDARD2_0
            return builder;
#else
            return builder.AddJsonProtocol(options => {
                options.PayloadSerializerOptions.Converters.AddDataCoreAdapterConverters();
            });
#endif
        }


#if NETSTANDARD2_0

        /// <summary>
        /// Maps adapter hub endpoints.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <returns>
        ///   The endpoint route builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="endpoints"/> is <see langword="null"/>.
        /// </exception>
        public static HubRouteBuilder MapDataCoreAdapterHubs(this HubRouteBuilder endpoints) {
            if (endpoints == null) {
                throw new ArgumentNullException(nameof(endpoints));
            }
            endpoints.MapHub<AdapterHub>(HubRoute);
            endpoints.MapHub<AdapterHub>(LegacyHubRoute);
            return endpoints;
        }

#else 

        /// <summary>
        /// Maps adapter hub endpoints.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <returns>
        ///   The endpoint route builder.
        /// </returns>
        public static IEndpointRouteBuilder MapDataCoreAdapterHubs(this IEndpointRouteBuilder endpoints) {
            return endpoints.MapDataCoreAdapterHubs(null);
        }


        /// <summary>
        /// Maps adapter hub endpoints.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <param name="builder">
        ///   A callback function that will be invoked for each hub that is registered with the 
        ///   host. The parameters are the type of the hub and the <see cref="HubEndpointConventionBuilder"/> 
        ///   for the hub endpoint registration. This can be used to e.g. require specific 
        ///   authentication schemes when hub connections are being established.
        /// </param>
        /// <returns>
        ///   The endpoint route builder.
        /// </returns>
        public static IEndpointRouteBuilder MapDataCoreAdapterHubs(this IEndpointRouteBuilder endpoints, Action<Type, HubEndpointConventionBuilder>? builder) {
            var hubEndpointBuilder = endpoints.MapHub<AdapterHub>(HubRoute);
            builder?.Invoke(typeof(AdapterHub), hubEndpointBuilder);

            hubEndpointBuilder = endpoints.MapHub<AdapterHub>(LegacyHubRoute);
            builder?.Invoke(typeof(AdapterHub), hubEndpointBuilder);

            return endpoints;
        }

#endif

    }
}
