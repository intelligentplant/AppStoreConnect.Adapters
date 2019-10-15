using System;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.AspNetCore {

    /// <summary>
    /// Options builder for registering adapter services.
    /// </summary>
    public class AdapterServicesOptionsBuilder {

        /// <summary>
        /// Describes the hosting ASP.NET Core application.
        /// </summary>
        public HostInfo HostInfo { get; set; }

        /// <summary>
        /// The <see cref="IAdapterAccessor"/> type to use.
        /// </summary>
        internal Type AdapterAccessorType { get; private set; }

        /// <summary>
        /// The <see cref="FeatureAuthorizationHandler"/> type to use.
        /// </summary>
        internal Type FeatureAuthorizationHandlerType { get; private set; }

        /// <summary>
        /// Indicates if API calls should be authorized.
        /// </summary>
        internal bool UseAuthorization { get; private set; }


        /// <summary>
        /// Sets the <see cref="IAdapterAccessor"/> to use.
        /// </summary>
        /// <typeparam name="TAccessor">
        ///   The <see cref="IAdapterAccessor"/> implementation.
        /// </typeparam>
        /// <returns>
        ///   The <see cref="AdapterServicesOptionsBuilder"/> instance.
        /// </returns>
        public AdapterServicesOptionsBuilder UseAdapterAccessor<TAccessor>() where TAccessor : class, IAdapterAccessor {
            AdapterAccessorType = typeof(TAccessor);

            return this;
        }


        /// <summary>
        /// Sets the <see cref="FeatureAuthorizationHandler"/> to use for authorization. Authorization 
        /// will not be applied if this method is not called during application startup.
        /// </summary>
        /// <typeparam name="THandler">
        ///   The <see cref="FeatureAuthorizationHandler"/> implementation.
        /// </typeparam>
        /// <returns>
        ///   The <see cref="AdapterServicesOptionsBuilder"/> instance.
        /// </returns>
        public AdapterServicesOptionsBuilder UseFeatureAuthorizationHandler<THandler>() where THandler : FeatureAuthorizationHandler {
            FeatureAuthorizationHandlerType = typeof(THandler);
            UseAuthorization = true;

            return this;
        }

    }
}
