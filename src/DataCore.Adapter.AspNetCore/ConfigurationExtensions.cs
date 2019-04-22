using System;
using DataCore.Adapter;
using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.AspNetCore.Hubs;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class ConfigurationExtensions {

        /// <summary>
        /// Adds services required to run App Store Connect adapters.
        /// </summary>
        /// <param name="services">
        ///   The service collection.
        /// </param>
        /// <param name="configure">
        ///   An <see cref="Action{T}"/> used to configure the adapter service options.
        /// </param>
        /// <returns>
        ///   The service collection.
        /// </returns>
        public static IServiceCollection AddDataCoreAdapterServices(this IServiceCollection services, Action<AdapterServicesOptionsBuilder> configure) {
            var options = new AdapterServicesOptionsBuilder();
            configure?.Invoke(options);

            if (options.AdapterAccessorType == null) {
                options.UseAdapterAccessor<AspNetCoreAdapterAccessor>();
            }

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAdapterCallContext, AdapterCallContext>();

            services.Add(new ServiceDescriptor(typeof(IAdapterAccessor), options.AdapterAccessorType, ServiceLifetime.Transient));

            services.AddSingleton(sp => new AdapterApiAuthorizationService(options.UseAuthorization, sp.GetService<AspNetCore.Authorization.IAuthorizationService>()));
            if (options.UseAuthorization) {
                services.Add(new ServiceDescriptor(typeof(AspNetCore.Authorization.IAuthorizationHandler), options.FeatureAuthorizationHandlerType, ServiceLifetime.Singleton));
            }

            services.AddSingleton<HostInfo>(sp => HostInfo.FromExisting(options.HostInfo ?? HostInfo.Unspecified));

            return services;
        }


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
            builder.AddApplicationPart(typeof(ConfigurationExtensions).Assembly);

            return builder;
        }


        /// <summary>
        /// Adds adapter hubs to the SignalR registration.
        /// </summary>
        /// <param name="builder">
        ///   The SignalR route builder.
        /// </param>
        /// <returns>
        ///   The SignalR route builder.
        /// </returns>
        public static HubRouteBuilder MapDataCoreAdapterHubs(this HubRouteBuilder builder) {
            builder.MapHub<RealTimeDataHub>("/signalr/v1.0/real-time-data");
            builder.MapHub<EventsHub>("/signalr/v1.0/events");
            return builder;
        }

    }

}
