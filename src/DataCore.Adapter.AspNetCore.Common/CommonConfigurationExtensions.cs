using System;
using DataCore.Adapter;
using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.Common;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class CommonConfigurationExtensions {

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

            services.AddBackgroundTaskService(options.BackgroundTaskServiceOptions);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAdapterCallContext, AdapterCallContext>();

            services.Add(new ServiceDescriptor(typeof(IAdapterAccessor), options.AdapterAccessorType, ServiceLifetime.Transient));

            services.AddSingleton(typeof(IAdapterAuthorizationService), sp => new AdapterAuthorizationService(options.UseAuthorization, sp.GetService<AspNetCore.Authorization.IAuthorizationService>()));
            if (options.UseAuthorization) {
                services.Add(new ServiceDescriptor(typeof(AspNetCore.Authorization.IAuthorizationHandler), options.FeatureAuthorizationHandlerType, ServiceLifetime.Singleton));
            }

            services.AddSingleton<HostInfo>(sp => HostInfo.FromExisting(options.HostInfo ?? HostInfo.Unspecified));
            services.AddHostedService<AdapterInitializer>();

            return services;
        }

    }

}
