using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using IntelligentPlant.BackgroundTasks;
using Microsoft.AspNetCore.SignalR;

namespace DataCore.Adapter.AspNetCore.Hubs {

    /// <summary>
    /// SignalR hub for adapter API calls.
    /// </summary>
    public partial class AdapterHub : Hub {

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
        protected IBackgroundTaskService TaskScheduler { get; }


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
        public AdapterHub(
            HostInfo hostInfo, 
            IAdapterAccessor adapterAccessor,
            IBackgroundTaskService taskScheduler
        ) {
            HostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
            AdapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
            TaskScheduler = taskScheduler ?? BackgroundTaskService.Default;
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
        /// <returns>
        ///   The matching adapters.
        /// </returns>
        public async Task<IEnumerable<AdapterDescriptor>> FindAdapters(FindAdaptersRequest request) {
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapters = await AdapterAccessor.FindAdapters(adapterCallContext, request, Context.ConnectionAborted).ConfigureAwait(false);
            return adapters.Select(x => AdapterDescriptor.FromExisting(x.Descriptor)).ToArray();
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await AdapterAccessor.GetAdapter(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            return adapter.CreateExtendedAdapterDescriptor();
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
            var adapterCallContext = new SignalRAdapterCallContext(Context);
            var adapter = await ResolveAdapterAndFeature<IHealthCheck>(adapterCallContext, adapterId, Context.ConnectionAborted).ConfigureAwait(false);
            return await adapter.Feature.CheckHealthAsync(adapterCallContext, Context.ConnectionAborted).ConfigureAwait(false);
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
        ///   The adapter does not support the requested feature.
        /// </exception>
        /// <exception cref="SecurityException">
        ///   The caller is not authorized to access the adapter feature.
        /// </exception>
        private async Task<ResolvedAdapterFeature<TFeature>> ResolveAdapterAndFeature<TFeature>(IAdapterCallContext adapterCallContext, string adapterId, CancellationToken cancellationToken) where TFeature : IAdapterFeature {
            var resolvedFeature = await AdapterAccessor.GetAdapterAndFeature<TFeature>(adapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                throw new ArgumentException(string.Format(adapterCallContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId), nameof(adapterId));
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
        private void ValidateObject(object instance) {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }

            Validator.ValidateObject(instance, new ValidationContext(instance), true);
        }


        /// <inheritdoc/>
        public override Task OnDisconnectedAsync(Exception exception) {
            OnTagValuesHubDisconnection();
            return base.OnDisconnectedAsync(exception);
        }


        /// <summary>
        /// Invoked when a client disconnects.
        /// </summary>
        partial void OnTagValuesHubDisconnection();

    }
}
