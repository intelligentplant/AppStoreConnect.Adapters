using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using DataCore.Adapter;
using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.Common;
using DataCore.Adapter.DependencyInjection;

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
        public static IAdapterConfigurationBuilder AddDataCoreAdapterAspNetCoreServices(this IServiceCollection services) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddDataCoreAdapterServices().AddDefaultAspNetCoreServices();
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
        ///   The vendor information for the hosting application. If <see langword="null"/>, the 
        ///   <see cref="VendorInfo"/> will be retrieved from the service provider. If this is also 
        ///   <see langword="null"/>, the method will look for a <see cref="VendorInfoAttribute"/> 
        ///   on the assembly returned by <see cref="Assembly.GetEntryAssembly"/> and will use that 
        ///   if available.
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
            string? description = null,
            string? version = null,
            VendorInfo? vendor = null,
            bool includeOperatingSystemDetails = true,
            bool includeContainerDetails = true,
            IEnumerable<AdapterProperty>? properties = null
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

            builder.Services.AddSingleton(sp => HostInfo.Create(
                name, 
                description,
                version ?? Assembly.GetEntryAssembly()?.GetInformationalVersion(),
                vendor ?? sp.GetService<VendorInfo>() ?? Assembly.GetEntryAssembly()?.GetCustomAttribute<VendorInfoAttribute>()?.CreateVendorInfo(),
                props
            ));
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
        /// Registers default adapter services.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddDefaultAspNetCoreServices(this IAdapterConfigurationBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddAspNetCoreBackgroundTaskService(options => options.AllowWorkItemRegistrationWhileStopped = true);
            builder.Services.TryAddSingleton(HostInfo.Unspecified);
            builder.AddAdapterAccessor<AspNetCoreAdapterAccessor>();
            builder.Services.AddSingleton(typeof(IAdapterAuthorizationService), sp => new DefaultAdapterAuthorizationService(false, sp.GetService<AspNetCore.Authorization.IAuthorizationService>()));
            builder.Services.TryAddTransient<IAvailableApiService, DefaultAvailableApiService>();
            builder.AddAutomaticInitialization();
            return builder;
        }

    }

}
