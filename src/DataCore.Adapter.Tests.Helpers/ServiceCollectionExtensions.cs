using System;

using DataCore.Adapter.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Tests {

    /// <summary>
    /// <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ServiceCollectionExtensions {

        /// <summary>
        /// Adds default App Store Connect adapter services.
        /// </summary>
        /// <param name="services">
        ///   The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configure">
        ///   A delegate that can be used to perform additional adapter-related configuration.
        /// </param>
        /// <returns>
        ///   The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddDefaultAdapterUnitTestServices(
            this IServiceCollection services, 
            Action<IAdapterConfigurationBuilder>? configure = null
        ) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddLogging();
            var adapterConfig = services.AddDataCoreAdapterServices();
            configure?.Invoke(adapterConfig);

            return services;
        }

    }
}
