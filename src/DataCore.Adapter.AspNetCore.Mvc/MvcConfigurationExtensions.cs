#if NETCOREAPP3_0
using DataCore.Adapter.Json;
#endif

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class MvcConfigurationExtensions {

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
            builder.AddApplicationPart(typeof(MvcConfigurationExtensions).Assembly);
#if NETCOREAPP3_0
            builder.AddJsonOptions(options => options.JsonSerializerOptions.Converters.AddAdapterConverters());
#endif

            return builder;
        }

    }

}
