using System;
using System.Collections.Generic;
using System.Linq;

using DataCore.Adapter;
using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Service registration extensions.
    /// </summary>
    public static class CommonAdapterConfigurationExtensions {

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
            builder.AddDefaultServices();

            return builder;
        }


        /// <summary>
        /// Adds information about the hosting application to the <see cref="IAdapterConfigurationBuilder"/>.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="hostInfo">
        ///   The <see cref="HostInfo"/> describing the host.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddHostInfo(
            this IAdapterConfigurationBuilder builder, 
            HostInfo hostInfo
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton(hostInfo ?? HostInfo.Unspecified);
            return builder;
        }


        /// <summary>
        /// Adds information about the hosting application to the <see cref="IAdapterConfigurationBuilder"/>.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="name">
        ///   The name of the hosting application.
        /// </param>
        /// <param name="description">
        ///   The description for the hosting application.
        /// </param>
        /// <param name="version">
        ///   The version of the hosting application.
        /// </param>
        /// <param name="vendor">
        ///   The vendor information for the hosting application.
        /// </param>
        /// <param name="includeOperatingSystemDetails">
        ///   When <see langword="true"/>, a property will be added to the host information 
        ///   specifying the operating system that the host is running on.
        /// </param>
        /// <param name="includeContainerDetails">
        ///   When <see langword="true"/>, a property will be added to the host information 
        ///   specifying if the host is running inside a container.
        /// </param>
        /// <param name="properties">
        ///   Additional properties to include in the host information.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public static IAdapterConfigurationBuilder AddHostInfo(
            this IAdapterConfigurationBuilder builder,
            string name,
            string description = null,
            string version = null,
            VendorInfo vendor = null,
            bool includeOperatingSystemDetails = true,
            bool includeContainerDetails = true,
            IEnumerable<AdapterProperty> properties = null
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException(Resources.Error_NameIsRequired, nameof(name));
            }

            var props = new List<AdapterProperty>();

            if (includeOperatingSystemDetails) {
                props.Add(AdapterProperty.Create(
                    Resources.HostProperty_OperatingSystem_Name,
                    System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    Resources.HostProperty_OperatingSystem_Description
                ));
            } 

            if (includeContainerDetails) {
                props.Add(AdapterProperty.Create(
                    Resources.HostProperty_IsRunningInContainer_Name,
                    Convert.ToBoolean(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), null),
                    Resources.HostProperty_IsRunningInContainer_Description
                ));
            }

            if (properties != null) {
                props.AddRange(properties);
            }
            var hostInfo = HostInfo.Create(name, description, version, vendor, props);

            return builder.AddHostInfo(hostInfo);
        }


        /// <summary>
        /// Adds the <see cref="IAdapterAccessor"/> service that is used to resolve adapters at 
        /// runtime.
        /// </summary>
        /// <typeparam name="T">
        ///   The <see cref="IAdapterAccessor"/> implementation type.
        /// </typeparam>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddAdapterAccessor<T>(
            this IAdapterConfigurationBuilder builder
        ) where T : class, IAdapterAccessor {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddAdapterAccessor(typeof(T));
        }


        /// <summary>
        /// Adds the <see cref="IAdapterAccessor"/> service that is used to resolve adapters at 
        /// runtime.
        /// </summary>
        /// <typeparam name="T">
        ///   The <see cref="IAdapterAccessor"/> implementation type.
        /// </typeparam>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="implementationFactory">
        ///   The factory that creates the service.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="implementationFactory"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddAdapterAccessor<T>(
            this IAdapterConfigurationBuilder builder,
            Func<IServiceProvider, T> implementationFactory
        ) where T : class, IAdapterAccessor {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (implementationFactory == null) {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            builder.Services.AddSingleton<IAdapterAccessor, T>(implementationFactory);
            return builder;
        }


        /// <summary>
        /// Adds the <see cref="IAdapterAccessor"/> service that is used to resolve adapters at 
        /// runtime.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="implementationType">
        ///   The <see cref="IAdapterAccessor"/> implementation type.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        private static IAdapterConfigurationBuilder AddAdapterAccessor(
            this IAdapterConfigurationBuilder builder,
            Type implementationType
        ) {
            builder.Services.AddSingleton(typeof(IAdapterAccessor), implementationType);
            return builder;
        }


        /// <summary>
        /// Adds the default background task service.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        private static IAdapterConfigurationBuilder AddBackgroundTaskService(
            this IAdapterConfigurationBuilder builder
        ) {
            builder.Services.AddBackgroundTaskService();
            return builder;
        }


        /// <summary>
        /// Configures the <see cref="IBackgroundTaskService"/> used by App Store Connect adapters.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="configure">
        ///   The configuration delegate.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddBackgroundTaskService(
            this IAdapterConfigurationBuilder builder,
            Action<BackgroundTaskServiceOptions> configure
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (configure == null) {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.AddBackgroundTaskService(configure);
            return builder;
        }


        /// <summary>
        /// Adds an adapter feature authorization service.
        /// </summary>
        /// <typeparam name="T">
        ///   The feature authorization handler implementation.
        /// </typeparam>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddAdapterFeatureAuthorization<T>(
            this IAdapterConfigurationBuilder builder
        ) where T : FeatureAuthorizationHandler {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddAdapterFeatureAuthorization(typeof(T));
        }


        /// <summary>
        /// Adds an adapter feature authorization service.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="implementationType">
        ///   The feature authorization handler implementation.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        private static IAdapterConfigurationBuilder AddAdapterFeatureAuthorization(
            this IAdapterConfigurationBuilder builder,
            Type implementationType
        ) {
            builder.Services.AddSingleton(typeof(IAdapterAuthorizationService), sp => new DefaultAdapterAuthorizationService(true, sp.GetService<AspNetCore.Authorization.IAuthorizationService>()));
            builder.Services.AddSingleton(typeof(AspNetCore.Authorization.IAuthorizationHandler), implementationType);
            return builder;
        }


        /// <summary>
        /// Adds a background service that will initialize all registered adapters at startup. See 
        /// the Remarks section for more information.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <remarks>
        ///   The automatic initialization service will initialize all adapters resolved by the 
        ///   registered <see cref="IAdapterAccessor"/> at startup that are marked as enabled.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddAutomaticInitialization(
            this IAdapterConfigurationBuilder builder
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            var registration = builder.Services.FirstOrDefault(x => x.ServiceType == typeof(IHostedService) && x.ImplementationType == typeof(AdapterInitializer));
            if (registration == null) {
                builder.Services.AddHostedService<AdapterInitializer>();
            }
            return builder;
        }


        /// <summary>
        /// Registers an App Store Connect adapter.
        /// </summary>
        /// <typeparam name="T">
        ///   The adapter implementation type.
        /// </typeparam>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   Adapters are registered as singleton services.
        /// </remarks>
        public static IAdapterConfigurationBuilder AddAdapter<T>(
            this IAdapterConfigurationBuilder builder
        ) where T : class, IAdapter {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IAdapter, T>();
            return builder;
        }


        /// <summary>
        /// Registers an App Store Connect adapter.
        /// </summary>
        /// <typeparam name="T">
        ///   The adapter implementation type.
        /// </typeparam>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="implementationFactory">
        ///   The factory that creates the service.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="implementationFactory"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   Adapters are registered as singleton services.
        /// </remarks>
        public static IAdapterConfigurationBuilder AddAdapter<T>(
            this IAdapterConfigurationBuilder builder,
            Func<IServiceProvider, T> implementationFactory
        ) where T : class, IAdapter {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (implementationFactory == null) {
                throw new ArgumentNullException(nameof(implementationFactory));
            }

            builder.Services.AddSingleton<IAdapter, T>(implementationFactory);
            return builder;
        }


        /// <summary>
        /// Registers additional services.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="configure">
        ///   A delegate that will register additional services.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="configure"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddServices(
            this IAdapterConfigurationBuilder builder,
            Action<IServiceCollection> configure
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (configure == null) {
                throw new ArgumentNullException(nameof(configure));
            }

            configure?.Invoke(builder.Services);

            return builder;
        }


        /// <summary>
        /// Registers default adapter services.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        private static IAdapterConfigurationBuilder AddDefaultServices(this IAdapterConfigurationBuilder builder) {
            builder.Services.AddSingleton(HostInfo.Unspecified);
            builder.AddAdapterAccessor<AspNetCoreAdapterAccessor>();
            builder.AddBackgroundTaskService();
            builder.Services.AddSingleton(typeof(IAdapterAuthorizationService), sp => new DefaultAdapterAuthorizationService(false, sp.GetService<AspNetCore.Authorization.IAuthorizationService>()));
            builder.AddAutomaticInitialization();
#if NETSTANDARD2_0
            builder.Services.TryAddSingleton<Newtonsoft.Json.JsonSerializerSettings>();
            builder.Services.AddTransient<IValueEncoder, DataCore.Adapter.NewtonsoftJson.NewtonsoftJsonValueEncoder>();
#else
            builder.Services.TryAddSingleton(sp => {
                var options = new System.Text.Json.JsonSerializerOptions();
                DataCore.Adapter.Json.JsonSerializerOptionsExtensions.AddDataCoreAdapterConverters(options.Converters);
                return options;
            });
            builder.Services.AddTransient<IValueEncoder, DataCore.Adapter.Json.JsonValueEncoder>();
#endif
            return builder;
        }


        /// <summary>
        /// Adds services required to run App Store Connect adapters.
        /// </summary>
        /// <param name="services">
        ///   The service collection.
        /// </param>
        /// <param name="configure">
        ///   An <see cref="Action{T}"/> used to configure the adapter service options.
        /// </param>
        /// <returns>
        ///   The service collection.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        [Obsolete("Use " + nameof(AddDataCoreAdapterServices) + "(" + nameof(IServiceCollection) + ") instead", false)]
        public static IServiceCollection AddDataCoreAdapterServices(this IServiceCollection services, Action<AdapterServicesOptionsBuilder> configure) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new AdapterServicesOptionsBuilder();
            configure?.Invoke(options);

            var builder = services.AddDataCoreAdapterServices();

            if (options.AdapterAccessorType != null) {
                builder.AddAdapterAccessor(options.AdapterAccessorType);
            }

            if (options.BackgroundTaskServiceOptions != null) {
                builder.AddBackgroundTaskService(options.BackgroundTaskServiceOptions);
            }

            if (options.UseAuthorization && options.FeatureAuthorizationHandlerType != null) {
                builder.AddAdapterFeatureAuthorization(options.FeatureAuthorizationHandlerType);
            }

            builder.AddAutomaticInitialization();
            builder.AddHostInfo(options.HostInfo);

            return services;
        }

    }

}
