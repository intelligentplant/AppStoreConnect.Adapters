using System;

using DataCore.Adapter;
using DataCore.Adapter.DependencyInjection;

using IntelligentPlant.BackgroundTasks;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Extensions for <see cref="IAdapterConfigurationBuilder"/>.
    /// </summary>
    public static class AdapterConfigurationBuilderExtensions {

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
        /// Adds <see cref="BackgroundTaskService.Default"/> as the registered <see cref="IBackgroundTaskService"/>.
        /// </summary>
        /// <param name="builder">
        ///   The configuration builder.
        /// </param>
        /// <returns>
        ///   The configuration builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddDefaultBackgroundTaskService(this IAdapterConfigurationBuilder builder) {
            return builder.AddBackgroundTaskService(BackgroundTaskService.Default);
        }


        /// <summary>
        /// Adds the specified implementation as the registered <see cref="IBackgroundTaskService"/>. 
        /// Note that the implementation must be externally initialised before it will be able to 
        /// process queued work items.
        /// </summary>
        /// <param name="builder">
        ///   The configuration builder.
        /// </param>
        /// <param name="implementationInstance">
        ///   The <see cref="IBackgroundTaskService"/> implementation instance to use.
        /// </param>
        /// <returns>
        ///   The configuration builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="implementationInstance"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddBackgroundTaskService(this IAdapterConfigurationBuilder builder, IBackgroundTaskService implementationInstance) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (implementationInstance == null) {
                throw new ArgumentNullException(nameof(implementationInstance));
            }

            builder.Services.AddSingleton(implementationInstance);

            return builder;
        }


        /// <summary>
        /// Adds an <see cref="IBackgroundTaskService"/> registration and supporting services to 
        /// the service collection. Note that the <see cref="IBackgroundTaskService"/> must be 
        /// externally initialised before it will be able to process queued work items.
        /// </summary>
        /// <typeparam name="T">
        ///   The background service implementation type.
        /// </typeparam>
        /// <param name="builder">
        ///   The configuration builder.
        /// </param>
        /// <param name="configure">
        ///   A delegate that can be used to configure the <see cref="BackgroundTaskServiceOptions"/> 
        ///   for the service.
        /// </param>
        /// <returns>
        ///   The configuration builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddBackgroundTaskService<T>(
            this IAdapterConfigurationBuilder builder,
            Action<BackgroundTaskServiceOptions>? configure = null
        ) where T : class, IBackgroundTaskService {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            var options = new BackgroundTaskServiceOptions();
            configure?.Invoke(options);
            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton<IBackgroundTaskService, T>();

            return builder;
        }

    }
}
