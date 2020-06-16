using System;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Default <see cref="IAdapterConfigurationBuilder"/> implementation.
    /// </summary>
    public class DefaultAdapterConfigurationBuilder : IAdapterConfigurationBuilder {

        /// <inheritdoc/>
        public IServiceCollection Services { get; }


        /// <summary>
        /// Creates anew <see cref="DefaultAdapterConfigurationBuilder"/> object.
        /// </summary>
        /// <param name="services">
        ///   The <see cref="IServiceCollection"/> where App Store Connect services are registered.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        public DefaultAdapterConfigurationBuilder(IServiceCollection services) {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

    }
}
