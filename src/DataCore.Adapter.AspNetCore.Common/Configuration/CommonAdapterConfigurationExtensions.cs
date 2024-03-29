﻿using System;
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

            return services.AddDataCoreAdapterServices()
                .AddDefaultAspNetCoreServices();
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hostInfo"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddHostInfo(
            this IAdapterConfigurationBuilder builder,
            HostInfo hostInfo
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (hostInfo == null) {
                throw new ArgumentNullException(nameof(hostInfo));
            }

            builder.Services.AddSingleton(hostInfo);
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
        ///   The version of the hosting application. If the version is <see langword="null"/> or 
        ///   cannot be parsed using strict SemVer v2.0 convensions, the version will be set by 
        ///   calling <see cref="DataCore.Adapter.AssemblyExtensions.GetInformationalVersion"/> on 
        ///   the assembly returned by <see cref="Assembly.GetEntryAssembly"/>.
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

            if (properties != null) {
                props.AddRange(properties.Where(x => x != null));
            }

            return builder.AddHostInfo((sp, hostInfoBuilder) => {
                hostInfoBuilder
                    .WithName(name)
                    .WithDescription(description);

                if (!includeOperatingSystemDetails || !includeContainerDetails) {
                    hostInfoBuilder.ClearProperties();
                    if (includeOperatingSystemDetails) {
                        AddOperatingSystemHostInfoProperty(hostInfoBuilder);
                    }
                    if (includeContainerDetails) {
                        AddContainerHostInfoProperty(hostInfoBuilder);
                    }
                }

                if (props.Count > 0) {
                    hostInfoBuilder.WithProperties(props);
                }

                if (version != null) {
                    hostInfoBuilder.WithVersion(version);
                }

                if (vendor != null) {
                    hostInfoBuilder.WithVendor(vendor);
                }
            });
        }


        /// <summary>
        /// Adds information about the hosting application to the <see cref="IAdapterConfigurationBuilder"/>.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="configure">
        ///   A delegate that is used to configure the <see cref="HostInfoBuilder"/> that builds 
        ///   the final <see cref="HostInfo"/> service.
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
        /// <remarks>
        /// 
        /// <para>
        ///   The <see cref="HostInfoBuilder"/> passed to the <paramref name="configure"/> 
        ///   delegate is pre-configured using the following default values:
        /// </para>
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Name</term>
        ///     <description>
        ///       The default host application name is set using the <see cref="AssemblyName.FullName"/> 
        ///       for the assembly returned by <see cref="Assembly.GetEntryAssembly"/>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>Version Number</term>
        ///     <description>
        ///       The default version number is obtained by calling <see cref="DataCore.Adapter.AssemblyExtensions.GetInformationalVersion"/> is 
        ///       on the assembly returned by <see cref="Assembly.GetEntryAssembly"/>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>Vendor Information</term>
        ///     <description>
        ///       The default vendor information is set using the <see cref="VendorInfo"/> service 
        ///       defined in the service provider. If the <see cref="VendorInfo"/> service cannot 
        ///       be resolved and the assembly returned by <see cref="Assembly.GetEntryAssembly"/> 
        ///       has a <see cref="VendorInfoAttribute"/> annotation, the details from the 
        ///       <see cref="VendorInfoAttribute"/> will be used.
        ///     </description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public static IAdapterConfigurationBuilder AddHostInfo(this IAdapterConfigurationBuilder builder, Action<HostInfoBuilder> configure) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (configure == null) {
                throw new ArgumentNullException(nameof(configure));
            }
            return builder.AddHostInfo((sp, hostInfoBuilder) => configure.Invoke(hostInfoBuilder));
        }


        /// <summary>
        /// Adds information about the hosting application to the <see cref="IAdapterConfigurationBuilder"/>.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="configure">
        ///   A delegate that is used to configure the <see cref="HostInfoBuilder"/> that builds 
        ///   the final <see cref="HostInfo"/> service.
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
        /// <remarks>
        /// 
        /// <para>
        ///   The <see cref="HostInfoBuilder"/> passed to the <paramref name="configure"/> 
        ///   delegate is pre-configured using the following default values:
        /// </para>
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Name</term>
        ///     <description>
        ///       The default host application name is set using the <see cref="AssemblyName.FullName"/> 
        ///       for the assembly returned by <see cref="Assembly.GetEntryAssembly"/>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>Version Number</term>
        ///     <description>
        ///       The default version number is obtained by calling <see cref="DataCore.Adapter.AssemblyExtensions.GetInformationalVersion"/> is 
        ///       on the assembly returned by <see cref="Assembly.GetEntryAssembly"/>.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term>Vendor Information</term>
        ///     <description>
        ///       The default vendor information is set using the <see cref="VendorInfo"/> service 
        ///       defined in the service provider. If the <see cref="VendorInfo"/> service cannot 
        ///       be resolved and the assembly returned by <see cref="Assembly.GetEntryAssembly"/> 
        ///       has a <see cref="VendorInfoAttribute"/> annotation, the details from the 
        ///       <see cref="VendorInfoAttribute"/> will be used.
        ///     </description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        public static IAdapterConfigurationBuilder AddHostInfo(this IAdapterConfigurationBuilder builder, Action<IServiceProvider, HostInfoBuilder> configure) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (configure == null) {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.Services.AddSingleton(sp => {
                var entryAssembly = Assembly.GetEntryAssembly();

                var hostInfoBuilder = new HostInfoBuilder()
                    .WithName(entryAssembly?.GetName()?.FullName)
                    .WithVersion(entryAssembly?.GetInformationalVersion())
                    .WithVendor(sp.GetService<VendorInfo>() ?? entryAssembly?.GetCustomAttribute<VendorInfoAttribute>()?.CreateVendorInfo());

                AddOperatingSystemHostInfoProperty(hostInfoBuilder);
                AddContainerHostInfoProperty(hostInfoBuilder);

                configure.Invoke(sp, hostInfoBuilder);
                return hostInfoBuilder.Build();
            });

            return builder;
        }


        /// <summary>
        /// Adds a host property that specifies the instance ID used by the host in distributed 
        /// telemetry systems.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="HostInfoBuilder"/>.
        /// </param>
        /// <param name="instanceId"></param>
        /// <returns>
        ///   The <see cref="HostInfoBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="instanceId"/> is <see langword="null"/> or white space.
        /// </exception>
        public static HostInfoBuilder WithInstanceId(this HostInfoBuilder builder, string instanceId) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(instanceId));
            }
            if (string.IsNullOrWhiteSpace(instanceId)) {
                throw new ArgumentOutOfRangeException(nameof(instanceId), Resources.Error_InstanceIdIsRequired);
            }

            AddInstanceIdProperty(builder, instanceId);

            return builder;
        }


        /// <summary>
        /// Adds a property that specifies the OS of the host.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="HostInfoBuilder"/>.
        /// </param>
        private static void AddOperatingSystemHostInfoProperty(HostInfoBuilder builder) {
            builder.WithProperties(new AdapterProperty(
                "OperatingSystem",
                System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                Resources.HostProperty_OperatingSystem_Description
            ));
        }


        /// <summary>
        /// Adds a property that specifies if the host is running inside a container.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="HostInfoBuilder"/>.
        /// </param>
        private static void AddContainerHostInfoProperty(HostInfoBuilder builder) {
            builder.WithProperties(new AdapterProperty(
                "Container",
                Convert.ToBoolean(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), null),
                Resources.HostProperty_IsRunningInContainer_Description
            ));
        }


        /// <summary>
        /// Adds a property that specifies the telemetry instance ID for the host.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="HostInfoBuilder"/>.
        /// </param>
        /// <param name="instanceId">
        ///   The telemetry instance ID.
        /// </param>
        private static void AddInstanceIdProperty(HostInfoBuilder builder, string instanceId) {
            builder.WithProperties(new AdapterProperty(
                "InstanceId",
                instanceId,
                Resources.HostProperty_InstanceId_Description
            ));
        }


        /// <summary>
        /// Adds <see cref="DefaultAdapterAuthorizationService"/> as the registered <see cref="IAdapterAuthorizationService"/>.
        /// </summary>
        /// <param name="services">
        ///   The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="requireAuthorization">
        ///   <see langword="true"/> to specify that the <see cref="DefaultAdapterAuthorizationService"/> 
        ///   must authorize access to adapters or <see langword="false"/> if authorization checks 
        ///   are not required (that is, no <see cref="FeatureAuthorizationHandler"/> has been 
        ///   registered by the hosting application).
        /// </param>
        /// <remarks>
        ///   The service is registered as a scoped service.
        /// </remarks>
        private static void AddDefaultAdapterAuthorizationService(this IServiceCollection services, bool requireAuthorization) {
            services.AddScoped(typeof(IAdapterAuthorizationService), sp => new DefaultAdapterAuthorizationService(requireAuthorization, sp.GetService<AspNetCore.Authorization.IAuthorizationService>()));
        }


        /// <summary>
        /// Tries to register <see cref="DefaultAdapterAuthorizationService"/> if an 
        /// <see cref="IAdapterAuthorizationService"/> has not already been registered.
        /// </summary>
        /// <param name="services">
        ///   The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="requireAuthorization">
        ///   <see langword="true"/> to specify that the <see cref="DefaultAdapterAuthorizationService"/> 
        ///   must authorize access to adapters or <see langword="false"/> if authorization checks 
        ///   are not required (that is, no <see cref="FeatureAuthorizationHandler"/> has been 
        ///   registered by the hosting application).
        /// </param>
        /// <remarks>
        ///   The service is registered as a scoped service.
        /// </remarks>
        private static void TryAddDefaultAdapterAuthorizationService(this IServiceCollection services, bool requireAuthorization) {
            services.TryAddScoped(typeof(IAdapterAuthorizationService), sp => new DefaultAdapterAuthorizationService(requireAuthorization, sp.GetService<AspNetCore.Authorization.IAuthorizationService>()));
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
            builder.Services.AddDefaultAdapterAuthorizationService(true);
            builder.Services.AddScoped(typeof(AspNetCore.Authorization.IAuthorizationHandler), implementationType);
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
            builder.Services.TryAddDefaultAdapterAuthorizationService(false);
            builder.Services.TryAddTransient<IAvailableApiService, DefaultAvailableApiService>();
            builder.AddAutomaticInitialization();
            return builder;
        }


        /// <summary>
        /// Registers an <see cref="IHostedService"/> that will request a graceful application 
        /// shutdown when any of the specified processes exit.
        /// </summary>
        /// <param name="services">
        ///   The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="pid">
        ///   The PID of the first process to watch.
        /// </param>
        /// <param name="additionalPids">
        ///   The PID of any additional processes to watch.
        /// </param>
        /// <returns>
        ///   The <see cref="IServiceCollection"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   This method is intended to allow adapter host applications that are started by an 
        ///   external application such as App Store Connect to exit if the external application 
        ///   exits without stopping the adapter host.
        /// </remarks>
        public static IServiceCollection AddDependentProcessWatcher(this IServiceCollection services, int pid, params int[] additionalPids) => AddDependentProcessWatcher(services, new[] { pid }.Concat(additionalPids));


        /// <summary>
        /// Registers an <see cref="IHostedService"/> that will request a graceful application 
        /// shutdown when any of the specified processes exit.
        /// </summary>
        /// <param name="services">
        ///   The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="pids">
        ///   The PIDs of the processes to watch.
        /// </param>
        /// <returns>
        ///   The <see cref="IServiceCollection"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="services"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pids"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   This method is intended to allow adapter host applications that are started by an 
        ///   external application such as App Store Connect to exit if the external application 
        ///   exits without stopping the adapter host.
        /// </remarks>
        public static IServiceCollection AddDependentProcessWatcher(this IServiceCollection services, IEnumerable<int> pids) {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }
            if (pids == null) {
                throw new ArgumentNullException(nameof(pids));
            }

            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<DependentProcessWatcher>(sp, pids));
            return services;
        }

    }

}
