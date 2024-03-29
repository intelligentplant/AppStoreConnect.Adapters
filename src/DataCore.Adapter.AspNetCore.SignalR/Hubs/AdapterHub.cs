﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;

using Microsoft.AspNetCore.SignalR;

namespace DataCore.Adapter.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub for adapter API calls.
    /// </summary>
    public partial class AdapterHub : Hub {

        /// <summary>
        /// Default channel capacity to use if <see cref="ChannelExtensions"/> does not contain a 
        /// specific <c>CreateXXXChannel</c> method for the channel item type.
        /// </summary>
        private const int DefaultChannelCapacity = 100;

        /// <summary>
        /// The host information.
        /// </summary>
        protected HostInfo HostInfo { get; }

        /// <summary>
        /// For accessing runtime adapters.
        /// </summary>
        protected IAdapterAccessor AdapterAccessor { get; }

        /// <summary>
        /// Task scheduler for running background operations.
        /// </summary>
        protected IBackgroundTaskService BackgroundTaskService { get; }

        /// <summary>
        /// JSON serialization options.
        /// </summary>
        private readonly System.Text.Json.JsonSerializerOptions? _jsonOptions;

        /// <summary>
        /// The <see cref="IServiceProvider"/>. Used to create the <see cref="SignalRAdapterCallContext"/> 
        /// for adapter invocations.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;


        /// <summary>
        /// Creates a new <see cref="AdapterHub"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The host information.
        /// </param>
        /// <param name="adapterAccessor">
        ///   For accessing runtime adapters.
        /// </param>
        /// <param name="taskScheduler">
        ///   The background task scheduler to use.
        /// </param>
        /// <param name="jsonOptions">
        ///   The configured JSON options.
        /// </param>
        /// <param name="serviceProvider">
        ///   The <see cref="IServiceProvider"/>. Used to create the <see cref="SignalRAdapterCallContext"/> 
        ///   for adapter invocations.
        /// </param>
        public AdapterHub(
            HostInfo hostInfo, 
            IAdapterAccessor adapterAccessor,
            IBackgroundTaskService taskScheduler,
            Microsoft.Extensions.Options.IOptions<JsonHubProtocolOptions> jsonOptions,
            IServiceProvider serviceProvider
        ) {
            HostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
            AdapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
            BackgroundTaskService = taskScheduler ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            _jsonOptions = jsonOptions?.Value?.PayloadSerializerOptions;
            _serviceProvider = serviceProvider;
        }


        /// <summary>
        /// Gets information about the adapter host.
        /// </summary>
        /// <returns>
        ///   The adapter host description.
        /// </returns>
        public HostInfo GetHostInfo() {
            return HostInfo;
        }


        /// <summary>
        /// Finds adapters matching the specified search filter.
        /// </summary>
        /// <param name="request">
        ///   The adapter search query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive the matching adapters.
        /// </returns>
        public async IAsyncEnumerable<AdapterDescriptor> FindAdapters(
            FindAdaptersRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);

            await foreach (var item in AdapterAccessor.FindAdapters(adapterCallContext, request, cancellationToken).ConfigureAwait(false)) {
                yield return AdapterDescriptor.FromExisting(item.Descriptor);
            }
        }


        /// <summary>
        /// Gets information about the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   Information about the requested adapter.
        /// </returns>
        public async Task<AdapterDescriptorExtended> GetAdapter(string adapterId) {
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var descriptor = await AdapterAccessor.GetAdapterDescriptorAsync(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            return descriptor!;
        }


        /// <summary>
        /// Performs a health check on the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   Information about the requested adapter.
        /// </returns>
        public async Task<HealthCheckResult> CheckAdapterHealth(string adapterId) {
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IHealthCheck>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);

            return await adapter.Feature.CheckHealthAsync(adapterCallContext, Context.ConnectionAborted).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a channel that will receive health check messages from the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that the subscriber can observe to receive health check messages.
        /// </returns>
        public async IAsyncEnumerable<HealthCheckResult> CreateAdapterHealthChannel(string adapterId, [EnumeratorCancellation] CancellationToken cancellationToken) {
            // Resolve the adapter and feature.
            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);
            var adapter = await ResolveAdapterAndFeature<IHealthCheck>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);

            await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Resolves an adapter and feature, and throws an exception if the adapter cannot be resolved, 
        /// or the caller is authorized to access the feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="adapterCallContext">
        ///   The adapter call context.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapter and feature.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> could not be resolved.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The adapter is not running or does not support the requested feature.
        /// </exception>
        /// <exception cref="SecurityException">
        ///   The caller is not authorized to access the adapter feature.
        /// </exception>
        private async Task<ResolvedAdapterFeature<TFeature>> ResolveAdapterAndFeature<TFeature>(IAdapterCallContext adapterCallContext, string adapterId, CancellationToken cancellationToken) where TFeature : IAdapterFeature {
            var resolvedFeature = await AdapterAccessor.GetAdapterAndFeature<TFeature>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId), nameof(adapterId));
            }

            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                throw new InvalidOperationException(string.Format(adapterCallContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId));
            }

            if (!resolvedFeature.IsFeatureResolved) {
                throw new InvalidOperationException(string.Format(adapterCallContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(TFeature)));
            }

            if (!resolvedFeature.IsFeatureAuthorized) {
                throw new SecurityException(Resources.Error_NotAuthorizedToAccessFeature);
            }

            return resolvedFeature;
        }


        /// <summary>
        /// Resolves an adapter and extension feature, and throws an exception if the adapter cannot be 
        /// resolved, or the caller is authorized to access the feature.
        /// </summary>
        /// <param name="adapterCallContext">
        ///   The adapter call context.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapter and feature.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> could not be resolved.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The adapter does not support the requested feature.
        /// </exception>
        /// <exception cref="SecurityException">
        ///   The caller is not authorized to access the adapter feature.
        /// </exception>
        [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
        private async Task<ResolvedAdapterFeature<IAdapterExtensionFeature>> ResolveAdapterAndExtensionFeature(IAdapterCallContext adapterCallContext, string adapterId, Uri featureUri, CancellationToken cancellationToken) {
            var resolvedFeature = await AdapterAccessor.GetAdapterAndFeature<IAdapterExtensionFeature>(adapterCallContext, adapterId, featureUri, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId), nameof(adapterId));
            }

            if (!resolvedFeature.IsFeatureResolved) {
                throw new InvalidOperationException(string.Format(adapterCallContext.CultureInfo, Resources.Error_UnsupportedInterface, featureUri));
            }

            if (!resolvedFeature.IsFeatureAuthorized) {
                throw new SecurityException(Resources.Error_NotAuthorizedToAccessFeature);
            }

            return resolvedFeature;
        }


        /// <summary>
        /// Validates the specified object. This method should be called on any adapter request objects 
        /// prior to passing them to an adapter.
        /// </summary>
        /// <param name="instance">
        ///   The object to validate.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="instance"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="instance"/> is not valid.
        /// </exception>
        private static void ValidateObject(object instance) {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }

            Validator.ValidateObject(instance, new ValidationContext(instance), true);
        }


        /// <inheritdoc/>
        public override Task OnDisconnectedAsync(Exception? exception) {
            return base.OnDisconnectedAsync(exception);
        }

    }
}
