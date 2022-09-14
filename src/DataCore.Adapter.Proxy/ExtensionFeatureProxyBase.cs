using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Proxy {

    /// <summary>
    /// Base class that a proxy adapter can inherit from when defining a proxy for an extension 
    /// feature.
    /// </summary>
    /// <typeparam name="TProxy">
    ///   The adapter proxy type.
    /// </typeparam>
    /// <typeparam name="TAdapterOptions">
    ///   The adapter proxy's options type.
    /// </typeparam>
    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
    public abstract class ExtensionFeatureProxyBase<TProxy, TAdapterOptions> : AdapterExtensionFeature where TProxy : AdapterBase<TAdapterOptions>, IAdapterProxy where TAdapterOptions : AdapterOptions, new() {

        /// <summary>
        /// Lazy-loaded feature URI for this instance.
        /// </summary>
        private readonly Lazy<Uri?> _featureUri;

        /// <summary>
        /// Gets the proxy adapter.
        /// </summary>
        protected TProxy Proxy { get; }


        /// <summary>
        /// Creates a new <see cref="ExtensionFeatureProxyBase{TProxy,TAdapterOptions}"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy adapter instance that owns the feature implementation.
        /// </param>
        /// <param name="encoders">
        ///   The <see cref="IObjectEncoder"/> instances to use when encoding or decoding 
        ///   extension objects.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="proxy"/> is <see langword="null"/>.
        /// </exception>
        protected ExtensionFeatureProxyBase(TProxy proxy, IEnumerable<IObjectEncoder> encoders) : base(proxy?.BackgroundTaskService, encoders) {
            Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            _featureUri = new Lazy<Uri?>(() => {
                return GetType()
                    .GetAdapterFeatureTypes()
                    .FirstOrDefault()
                    ?.GetAdapterFeatureUri();
            }, LazyThreadSafetyMode.PublicationOnly);
        }


        /// <inheritdoc/>
        protected sealed override Task<FeatureDescriptor?> GetDescriptorInternal(
            IAdapterCallContext context, 
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            return GetDescriptorFromRemoteAdapter(context, _featureUri.Value, cancellationToken);
        }


        /// <inheritdoc/>
        protected sealed override Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperationsInternal(
            IAdapterCallContext context, 
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            return GetOperationsFromRemoteAdapter(context, _featureUri.Value, cancellationToken);
        }


        /// <summary>
        /// Gets the descriptor for the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="featureUri">
        ///   The URI of the extension feature to retrieve the descriptor for.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The extension feature descriptor.
        /// </returns>
        protected abstract Task<FeatureDescriptor?> GetDescriptorFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Gets the operations that are supported by the extension feature.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="featureUri">
        ///   The URI of the extension feature to retrieve the operations for.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The operation descriptors.
        /// </returns>
        protected abstract Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperationsFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        );

    }
}
