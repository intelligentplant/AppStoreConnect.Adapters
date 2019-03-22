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

    public static class DataCoreAdapterExtensions {

        public static IServiceCollection AddDataCoreAdapterServices<TAdapterAccessor>(this IServiceCollection services) where TAdapterAccessor : class, IAdapterAccessor {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IDataCoreContext, DataCoreContext>();
            services.AddScoped<IAdapterAccessor, TAdapterAccessor>();

            return services;
        }


        public static IMvcBuilder AddDataCoreAdapterMvc(this IMvcBuilder builder) {
            builder.AddApplicationPart(typeof(DataCoreAdapterExtensions).Assembly);

            return builder;
        }

    }

}
