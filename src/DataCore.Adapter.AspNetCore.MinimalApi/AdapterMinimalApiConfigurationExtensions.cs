using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Internal;
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
            services.AddApiVersioning();
            services.AddTransient<IApiDescriptorProvider, ApiDescriptorProvider>();

            services.Configure<JsonOptions>(options => options.SerializerOptions.UseDataCoreAdapterDefaults());
            return services;
        }

    }
}
