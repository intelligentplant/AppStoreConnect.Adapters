using System;
using DataCore.Adapter.AspNetCore.Hubs;

#if NETCOREAPP3_1
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
        public const string HubRoute = "/signalr/data-core/v1.0";


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
#if NETCOREAPP3_1
            return builder.AddJsonProtocol(options => {
                options.PayloadSerializerOptions.Converters.AddDataCoreAdapterConverters();
            });
#else
            return builder;
#endif
        }

#if NETCOREAPP3_1

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
        public static IEndpointRouteBuilder MapDataCoreAdapterHubs(this IEndpointRouteBuilder endpoints, Action<Type, HubEndpointConventionBuilder> builder) {
            var hubEndpointBuilder = endpoints.MapHub<AdapterHub>(HubRoute);
            builder?.Invoke(typeof(AdapterHub), hubEndpointBuilder);
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="endpoints"/> is <see langword="null"/>.
        /// </exception>
        public static HubRouteBuilder MapDataCoreAdapterHubs(this HubRouteBuilder endpoints) {
            if (endpoints == null) {
                throw new ArgumentNullException(nameof(endpoints));
            }
            endpoints.MapHub<AdapterHub>(HubRoute);
            return endpoints;
        }

#endif

    }
}
