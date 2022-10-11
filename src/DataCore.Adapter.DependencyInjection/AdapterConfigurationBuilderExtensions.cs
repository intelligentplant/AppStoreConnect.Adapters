using System;
using System.Linq;

using DataCore.Adapter;
using DataCore.Adapter.DependencyInjection;
using DataCore.Adapter.Services;

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


        /// <summary>
        /// Adds a singleton <see cref="IKeyValueStore"/> registration to the service collection.
        /// </summary>
        /// <param name="builder">
        ///   The configuration builder.
        /// </param>
        /// <param name="implementationInstance">
        ///   The <see cref="IKeyValueStore"/> implementation instance to use.
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
        public static IAdapterConfigurationBuilder AddKeyValueStore(
            this IAdapterConfigurationBuilder builder,
            IKeyValueStore implementationInstance
        ) {
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
        /// Adds a singleton <see cref="IKeyValueStore"/> registration to the service collection.
        /// </summary>
        /// <typeparam name="T">
        ///   The <see cref="IKeyValueStore"/> implementation type.
        /// </typeparam>
        /// <param name="builder">
        ///   The configuration builder.
        /// </param>
        /// <returns>
        ///   The configuration builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddKeyValueStore<T>(this IAdapterConfigurationBuilder builder) where T : class, IKeyValueStore {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IKeyValueStore, T>();
            return builder;
        }


        /// <summary>
        /// Adds a singleton <see cref="IKeyValueStore"/> registration to the service collection.
        /// </summary>
        /// <typeparam name="T">
        ///   The <see cref="IKeyValueStore"/> implementation type.
        /// </typeparam>
        /// <param name="builder">
        ///   The configuration builder.
        /// </param>
        /// <param name="implementationFactory">
        ///   The implementation factory to use.
        /// </param>
        /// <returns>
        ///   The configuration builder.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="implementationFactory"/> is <see langword="null"/>.
        /// </exception>
        public static IAdapterConfigurationBuilder AddKeyValueStore<T>(this IAdapterConfigurationBuilder builder, Func<IServiceProvider, T> implementationFactory) where T : class, IKeyValueStore {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (implementationFactory == null) {
                throw new ArgumentNullException(nameof(implementationFactory));
            }
            
            builder.Services.AddSingleton<IKeyValueStore, T>(implementationFactory);
            return builder;
        }


        /// <summary>
        /// Registers adapter options for the specified adapter ID.
        /// </summary>
        /// <typeparam name="TOptions">
        ///   The adapter options type.
        /// </typeparam>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter that the <typeparamref name="TOptions"/> are being registered 
        ///   for.
        /// </param>
        /// <param name="configure">
        ///   An optional callback for configuring the <typeparamref name="TOptions"/> (for example, 
        ///   by binding them to an <see cref="Configuration.IConfiguration"/> instance).
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterId"/> is <see langword="null"/>
        /// </exception>
        /// <remarks>
        ///   A named options instance using the <paramref name="adapterId"/> will be registered.
        /// </remarks>
        public static IAdapterConfigurationBuilder AddAdapterOptions<TOptions>(this IAdapterConfigurationBuilder builder, string adapterId, Action<Options.OptionsBuilder<TOptions>>? configure = null) where TOptions : AdapterOptions, new() {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (adapterId == null) {
                throw new ArgumentNullException(nameof(adapterId));
            }

            var optionsBuilder = builder.Services.AddOptions<TOptions>(adapterId);
            configure?.Invoke(optionsBuilder);

            return builder;
        }


        /// <summary>
        /// Registers a singleton App Store Connect adapter using the specified direct constructor 
        /// arguments in addition to those provided by the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The adapter implementation type.
        /// </typeparam>
        /// <param name="builder">
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </param>
        /// <param name="additionalConstructorParameters">
        ///   Direct constructor arguments to use in addition to those supplied by the 
        ///   <see cref="IServiceProvider"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="IAdapterConfigurationBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///
        /// <para>
        ///   This overload registers the adapter using an implementation factory that calls 
        ///   <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/>.
        /// </para>
        /// 
        /// <para>
        ///   To register an adapter using a custom implementation factory, call 
        ///   <see cref="AddAdapter{T}(IAdapterConfigurationBuilder, Func{IServiceProvider, T})"/>.
        /// </para>
        ///
        /// </remarks>
        public static IAdapterConfigurationBuilder AddAdapter<T>(
            this IAdapterConfigurationBuilder builder,
            params object[] additionalConstructorParameters
        ) where T : class, IAdapter {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<IAdapter, T>(sp => ActivatorUtilities.CreateInstance<T>(sp, additionalConstructorParameters));

            return builder;
        }


        /// <summary>
        /// Registers a singleton App Store Connect adapter using the specified implementation 
        /// factory.
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

    }
}
