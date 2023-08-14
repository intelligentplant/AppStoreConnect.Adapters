using System;
using DataCore.Adapter.AspNetCore.Hubs;
using DataCore.Adapter.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class SignalRConfigurationExtensions {

        /// <summary>
        /// SignalR Hub route.
        /// </summary>
        public const string HubRoute = "/signalr/app-store-connect/v2.0";


        /// <summary>
        /// Configures services required for adapter SignalR.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="ISignalRServerBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="ISignalRServerBuilder"/>.
        /// </returns>
        public static ISignalRServerBuilder AddDataCoreAdapterSignalR(this ISignalRServerBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddJsonProtocol(options => {
                options.PayloadSerializerOptions.UseDataCoreAdapterDefaults();
            });

            builder.Services.AddTransient<DataCore.Adapter.AspNetCore.IApiDescriptorProvider, DataCore.Adapter.AspNetCore.SignalR.Internal.ApiDescriptorProvider>();
            return builder;
        }


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
            return endpoints.MapDataCoreAdapterHubs(null, null);
        }


        /// <summary>
        /// Maps adapter hub endpoints.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the hub route.
        /// </param>
        /// <returns>
        ///   The endpoint route builder.
        /// </returns>
        public static IEndpointRouteBuilder MapDataCoreAdapterHubs(this IEndpointRouteBuilder endpoints, PathString? prefix) {
            return endpoints.MapDataCoreAdapterHubs(prefix, null);
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
            return endpoints.MapDataCoreAdapterHubs(null, builder);
        }


        /// <summary>
        /// Maps adapter hub endpoints.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <param name="prefix">
        ///   The prefix for the hub route.
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
        public static IEndpointRouteBuilder MapDataCoreAdapterHubs(this IEndpointRouteBuilder endpoints, PathString? prefix, Action<Type, HubEndpointConventionBuilder>? builder) {
            var route = prefix == null
                ? HubRoute
                : prefix.Value.Add(new PathString(HubRoute)).ToString();
            
            var hubEndpointBuilder = endpoints.MapHub<AdapterHub>(route);
            builder?.Invoke(typeof(AdapterHub), hubEndpointBuilder);

            return endpoints;
        }

    }
}
