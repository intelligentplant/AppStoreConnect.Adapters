using System;

using DataCore.Adapter.Json;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class MvcConfigurationExtensions {

        /// <summary>
        /// Adds the adapter API controllers to the MVC registration.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IMvcBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IMvcBuilder"/>.
        /// </returns>
        public static IMvcBuilder AddDataCoreAdapterMvc(this IMvcBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddApplicationPart(typeof(MvcConfigurationExtensions).Assembly);
            builder.Services.AddTransient<DataCore.Adapter.AspNetCore.IApiDescriptorProvider, DataCore.Adapter.AspNetCore.Mvc.Internal.ApiDescriptorProvider>();
            builder.AddJsonOptions(options => {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                options.JsonSerializerOptions.AddDataCoreAdapterContext();
            });

            return builder;
        }

    }

}
