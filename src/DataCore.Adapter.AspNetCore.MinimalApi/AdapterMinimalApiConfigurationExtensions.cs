using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.DependencyInjection;
using DataCore.Adapter.Json;

using Microsoft.AspNetCore.Http.Json;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Extensions for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class AdapterMinimalApiConfigurationExtensions {

        /// <summary>
        /// Registers services used by the adapter Minimal API routes.
        /// </summary>
        /// <param name="services">
        ///   The <see cref="IServiceCollection"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddDataCoreAdapterApiServices(this IServiceCollection services) {
            services.AddTransient<IApiDescriptorProvider, ApiDescriptorProvider>();

            services.Configure<JsonOptions>(options => options.SerializerOptions.UseDataCoreAdapterDefaults());
            return services;
        }


        /// <summary>
        /// Registers services used by the adapter Minimal API routes.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        public static IAdapterConfigurationBuilder AddDataCoreAdapterApiServices(this IAdapterConfigurationBuilder builder) {
            builder.Services.AddDataCoreAdapterApiServices();
            return builder;
        }

    }
}
