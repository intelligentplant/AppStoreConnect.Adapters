using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
        ///   A channel that will return the available adapters.
        /// </returns>
        public static IAsyncEnumerable<IAdapter> GetAllAdapters(
            this IAdapterAccessor adapterAccessor, 
            IAdapterCallContext context, 
            CancellationToken cancellationToken = default
        ) {
            if (adapterAccessor == null) {
                throw new ArgumentNullException(nameof(adapterAccessor));
            }

            return adapterAccessor.FindAdapters(context, new Common.FindAdaptersRequest() { 
                Page = 1,
                PageSize = int.MaxValue
            }, cancellationToken);
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "Parameter is not guaranteed to be a URI")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "Parameter is not guaranteed to be a URI")]
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
