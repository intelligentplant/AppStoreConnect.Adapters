using System;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// An interface for configuring App Store Connect adapter services.
    /// </summary>
    public interface IAdapterConfigurationBuilder {

        /// <summary>
        /// The <see cref="IServiceCollection"/> where App Store Connect adapter services are 
        /// registered.
        /// </summary>
        IServiceCollection Services { get; }

    }
}
