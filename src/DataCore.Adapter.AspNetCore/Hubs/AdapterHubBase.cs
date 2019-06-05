using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Authorization;
using DataCore.Adapter.Common.Models;
using Microsoft.AspNetCore.SignalR;

namespace DataCore.Adapter.AspNetCore.Hubs {

    /// <summary>
    /// Base class that all adapter hubs should inherit from.
    /// </summary>
    public abstract class AdapterHubBase : Hub {

        /// <summary>
        /// The host information.
        /// </summary>
        protected HostInfo HostInfo { get; }

        /// <summary>
        /// The adapter call context describing the calling user.
        /// </summary>
        protected IAdapterCallContext AdapterCallContext { get; }

        /// <summary>
        /// For accessing runtime adapters.
        /// </summary>
        protected IAdapterAccessor AdapterAccessor { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterHubBase"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The host information.
        /// </param>
        /// <param name="adapterCallContext">
        ///   The adapter call context describing the calling user.
        /// </param>
        /// <param name="adapterAccessor">
        ///   For accessing runtime adapters.
        /// </param>
        protected AdapterHubBase(HostInfo hostInfo, IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            HostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
            AdapterCallContext = adapterCallContext ?? throw new ArgumentNullException(nameof(adapterCallContext));
            AdapterAccessor = adapterAccessor ?? throw new ArgumentNullException(nameof(adapterAccessor));
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
        /// Gets information about the available adapters.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapters visible to the caller.
        /// </returns>
        public async Task<IEnumerable<AdapterDescriptor>> GetAdapters(CancellationToken cancellationToken) {
            var adapters = await AdapterAccessor.GetAdapters(AdapterCallContext, cancellationToken).ConfigureAwait(false);
            return adapters.Select(x => x.Descriptor).ToArray();
        }


        /// <summary>
        /// Gets information about the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   Information about the requested adapter.
        /// </returns>
        public async Task<AdapterDescriptorExtended> GetAdapter(string adapterId, CancellationToken cancellationToken) {
            var adapter = await AdapterAccessor.GetAdapter(AdapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            return adapter.CreateExtendedAdapterDescriptor();
        }


        /// <summary>
        /// Resolves the specified adapter, and throws an exception if the adapter cannot be resolved.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The adapter.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> could not be resolved.
        /// </exception>
        protected async Task<IAdapter> ResolveAdapter(string adapterId, CancellationToken cancellationToken) {
            var adapter = await AdapterAccessor.GetAdapter(AdapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                throw new ArgumentException(string.Format(Resources.Error_CannotResolveAdapterId, adapterId), nameof(adapterId));
            }

            return adapter;
        }


        /// <summary>
        /// Resolves an adapter and feature, and throws an exception if the adapter cannot be resolved, 
        /// or the caller is authorized to access the feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
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
        protected async Task<ResolvedAdapterFeature<TFeature>> ResolveAdapterAndFeature<TFeature>(string adapterId, CancellationToken cancellationToken) where TFeature : IAdapterFeature {
            var resolvedFeature = await AdapterAccessor.GetAdapterAndFeature<TFeature>(AdapterCallContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                throw new ArgumentException(string.Format(Resources.Error_CannotResolveAdapterId, adapterId), nameof(adapterId));
            }

            if (!resolvedFeature.IsFeatureResolved) {
                throw new InvalidOperationException(string.Format(Resources.Error_UnsupportedInterface, nameof(TFeature)));
            }

            if (!resolvedFeature.IsAuthorized) {
                throw new SecurityException(Resources.Error_NotAuthorizedToAccessFeature);
            }

            return resolvedFeature;
        }

    }
}
