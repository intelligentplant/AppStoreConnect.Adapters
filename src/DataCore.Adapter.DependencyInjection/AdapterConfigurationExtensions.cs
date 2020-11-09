using System;

using DataCore.Adapter.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// DI extensions for configuring adapter services.
    /// </summary>
    public static class AdapterConfigurationExtensions {

        /// <summary>
        /// Adds App Store Connect adapter services to the service collection.
        /// </summary>
        /// <param name="services">
        ///   The service collection.
        /// </param>
        /// <returns>
        ///   An <see cref="IAdapterConfigurationBuilder"/> that can be used to further configure 
        ///   the App Store Connect adapter services.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddDataCoreAdapterServices(this IServiceCollection services) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            var builder = new DefaultAdapterConfigurationBuilder(services);
            builder.AddDefaultBackgroundTaskService();

            return builder;
        }

    }
}
