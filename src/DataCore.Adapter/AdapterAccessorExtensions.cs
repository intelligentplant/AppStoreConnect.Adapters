using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapterAccessor"/>.
    /// </summary>
    public static class AdapterAccessorExtensions {

        /// <summary>
        /// Gets all adapters registered with the <see cref="IAdapterAccessor"/>.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The <see cref="IAdapterAccessor"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the available adapters.
        /// </returns>
        public static async IAsyncEnumerable<IAdapter> GetAllAdapters(
            this IAdapterAccessor adapterAccessor, 
            IAdapterCallContext context,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (adapterAccessor == null) {
                throw new ArgumentNullException(nameof(adapterAccessor));
            }

            var page = 0;

            while (true) {
                var @continue = false;

                await foreach (var adapter in adapterAccessor.FindAdapters(context, new FindAdaptersRequest() {
                    Page = ++page,
                    PageSize = 500
                }, cancellationToken).ConfigureAwait(false)) {
                    yield return adapter;
                    @continue = true;
                }

                if (!@continue) {
                    break;
                }
            }
        }


        /// <summary>
        /// Gets a <see cref="AdapterDescriptorExtended"/> for the specified adapter that reflects 
        /// the runtime permissions of the calling <see cref="IAdapterCallContext"/>.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The <see cref="IAdapterAccessor"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the calling user.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the requested <see cref="AdapterDescriptorExtended"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterAccessor"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapterId"/> is <see langword="null"/>.
        /// </exception>
        public static async Task<AdapterDescriptorExtended?> GetAdapterDescriptorAsync(
            this IAdapterAccessor adapterAccessor,
            IAdapterCallContext context,
            string adapterId,
            CancellationToken cancellationToken = default
        ) {
            if (adapterAccessor == null) {
                throw new ArgumentNullException(nameof(adapterAccessor));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (adapterId == null) {
                throw new ArgumentNullException(nameof(adapterId));
            }

            var adapter = await adapterAccessor.GetAdapter(context, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return null;
            }

            var builder = adapter.CreateExtendedAdapterDescriptorBuilder();
            foreach (var featureUri in adapter.Features.Keys) {
                var isAuthorized = await adapterAccessor.AuthorizationService.AuthorizeAdapterFeature(adapter, context, featureUri, cancellationToken).ConfigureAwait(false);
                if (!isAuthorized) {
                    builder.ClearFeature(featureUri);
                }
            }

            return builder.Build();
        }


        /// <summary>
        /// Resolves the specified adapter and feature, and verifies if the caller is authorized 
        /// to access the feature. The adapter must be enabled.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The adapter feature.
        /// </typeparam>
        /// <param name="adapterAccessor">
        ///   The <see cref="IAdapterAccessor"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ResolvedAdapterFeature{TFeature}"/> describing the adapter, feature, and 
        ///   authorization result.
        /// </returns>
        public static async Task<ResolvedAdapterFeature<TFeature>> GetAdapterAndFeature<TFeature>(
            this IAdapterAccessor adapterAccessor, 
            IAdapterCallContext context, 
            string adapterId, 
            Uri featureUri,
            CancellationToken cancellationToken = default
        ) where TFeature : IAdapterFeature {
            if (adapterAccessor == null) {
                throw new ArgumentNullException(nameof(adapterAccessor));
            }
            if (featureUri == null) {
                throw new ArgumentNullException(nameof(featureUri));
            }

            using (Diagnostics.Telemetry.ActivitySource.StartActivity("GetAdapterAndFeature")) {
                var adapter = await adapterAccessor.GetAdapter(context, adapterId, cancellationToken).ConfigureAwait(false);
                if (adapter == null || !adapter.IsEnabled) {
                    return new ResolvedAdapterFeature<TFeature>(null!, default!, false);
                }

                var feature = adapter.GetFeature<TFeature>(featureUri);
                if (feature == null) {
                    return new ResolvedAdapterFeature<TFeature>(adapter, default!, false);
                }

                var isAuthorized = await adapterAccessor.AuthorizationService.AuthorizeAdapterFeature(adapter, context, featureUri, cancellationToken).ConfigureAwait(false);
                return new ResolvedAdapterFeature<TFeature>(adapter, feature, isAuthorized);
            }
        }



        /// <summary>
        /// Resolves the specified adapter and feature, and verifies if the caller is authorized 
        /// to access the feature. The adapter must be enabled.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The adapter feature.
        /// </typeparam>
        /// <param name="adapterAccessor">
        ///   The <see cref="IAdapterAccessor"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ResolvedAdapterFeature{TFeature}"/> describing the adapter, feature, and 
        ///   authorization result.
        /// </returns>
        public static async Task<ResolvedAdapterFeature<TFeature>> GetAdapterAndFeature<TFeature>(
            this IAdapterAccessor adapterAccessor, 
            IAdapterCallContext context, 
            string adapterId,  
            CancellationToken cancellationToken = default
        ) where TFeature : IAdapterFeature {
            if (adapterAccessor == null) {
                throw new ArgumentNullException(nameof(adapterAccessor));
            }

            using (Diagnostics.Telemetry.ActivitySource.StartActivity("GetAdapterAndFeature")) {
                var adapter = await adapterAccessor.GetAdapter(context, adapterId, cancellationToken).ConfigureAwait(false);
                if (adapter == null || !adapter.IsEnabled) {
                    return new ResolvedAdapterFeature<TFeature>(null!, default!, false);
                }

                var uri = typeof(TFeature).GetAdapterFeatureUri();

                if (uri == null || !adapter.TryGetFeature<TFeature>(uri, out var feature)) {
                    return new ResolvedAdapterFeature<TFeature>(adapter, default!, false);
                }

                var isAuthorized = await adapterAccessor.AuthorizationService.AuthorizeAdapterFeature(adapter, context, uri, cancellationToken).ConfigureAwait(false);
                return new ResolvedAdapterFeature<TFeature>(adapter, feature!, isAuthorized);
            }
        }


        /// <summary>
        /// Resolves the specified adapter and feature, and verifies if the caller is authorized 
        /// to access the feature. The adapter must be enabled.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The <see cref="IAdapterAccessor"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ResolvedAdapterFeature{TFeature}"/> describing the adapter, feature, and 
        ///   authorization result.
        /// </returns>
        public static Task<ResolvedAdapterFeature<IAdapterFeature>> GetAdapterAndFeature(
            this IAdapterAccessor adapterAccessor,
            IAdapterCallContext context,
            string adapterId,
            Uri featureUri,
            CancellationToken cancellationToken = default
        ) {
            return adapterAccessor.GetAdapterAndFeature<IAdapterFeature>(
                context,
                adapterId,
                featureUri,
                cancellationToken
            );
        }


        /// <summary>
        /// Resolves the specified adapter and feature, and verifies if the caller is authorized 
        /// to access the feature. The adapter must be enabled.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The <see cref="IAdapterAccessor"/>.
        /// </param>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterId">
        ///   The ID of the adapter.
        /// </param>
        /// <param name="featureUri">
        ///   The feature URI.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ResolvedAdapterFeature{TFeature}"/> describing the adapter, feature, and 
        ///   authorization result.
        /// </returns>
        public static Task<ResolvedAdapterFeature<IAdapterFeature>> GetAdapterAndFeature(
            this IAdapterAccessor adapterAccessor, 
            IAdapterCallContext context, 
            string adapterId, 
            string featureUri, 
            CancellationToken cancellationToken = default
        ) {
            return adapterAccessor.GetAdapterAndFeature<IAdapterFeature>(
                context, 
                adapterId,
                featureUri.TryCreateUriWithTrailingSlash(out var uri)
                    ? uri!
                    : throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(featureUri)), 
                cancellationToken
            );
        }

    }
}
