using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Http.Proxy.Extensions {
    /// <summary>
    /// Allows communication with remote <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    public class AdapterExtensionFeatureImpl : AdapterExtensionFeature {

        /// <summary>
        /// The owning proxy.
        /// </summary>
        private readonly HttpAdapterProxy _proxy;


        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeatureImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="proxy"/> is <see langword="null"/>.
        /// </exception>
        public AdapterExtensionFeatureImpl(HttpAdapterProxy proxy) {
            _proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }


        /// <inheritdoc/>
        protected override Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
            IAdapterCallContext context,
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            var client = _proxy.GetClient();
            return client.Extensions.GetOperationsAsync(_proxy.RemoteDescriptor.Id, featureUri, context?.ToRequestMetadata(), cancellationToken);
        }


        /// <inheritdoc/>
        protected override Task<string> Invoke(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            var client = _proxy.GetClient();
            return client.Extensions.InvokeExtensionAsync(_proxy.RemoteDescriptor.Id, operationId, argument, context?.ToRequestMetadata(), cancellationToken);
        }

    }
}
