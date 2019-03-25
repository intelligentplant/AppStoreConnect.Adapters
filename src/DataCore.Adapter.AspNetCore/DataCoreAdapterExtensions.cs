using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataCore.Adapter;
using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.DataSource;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class DataCoreAdapterExtensions {

        /// <summary>
        /// Adds services required to run App Store Connect adapters.
        /// </summary>
        /// <typeparam name="TAdapterAccessor">
        ///   The <see cref="IAdapterAccessor"/> implementation type to use. The <see cref="IAdapterAccessor"/> 
        ///   is registered as a trasient service.
        /// </typeparam>
        /// <param name="services">
        ///   The service collection.
        /// </param>
        /// <returns>
        ///   The service collection.
        /// </returns>
        public static IServiceCollection AddDataCoreAdapterServices<TAdapterAccessor>(this IServiceCollection services) where TAdapterAccessor : class, IAdapterAccessor {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IDataCoreContext, DataCoreContext>();
            services.AddTransient<IAdapterAccessor, TAdapterAccessor>();

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
            builder.AddApplicationPart(typeof(DataCoreAdapterExtensions).Assembly);

            return builder;
        }

    }

}
