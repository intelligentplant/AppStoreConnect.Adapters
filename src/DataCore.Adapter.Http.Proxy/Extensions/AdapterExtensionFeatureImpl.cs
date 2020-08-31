using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.Proxy;

namespace DataCore.Adapter.Http.Proxy.Extensions {
    /// <summary>
    /// Allows communication with remote <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    public class AdapterExtensionFeatureImpl : ExtensionFeatureProxyBase<HttpAdapterProxy> {

        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeatureImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="proxy"/> is <see langword="null"/>.
        /// </exception>
        public AdapterExtensionFeatureImpl(HttpAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        protected override Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
            IAdapterCallContext context,
            Uri featureUri,
            CancellationToken cancellationToken
        ) {
            var client = Proxy.GetClient();
            return client.Extensions.GetOperationsAsync(
                Proxy.RemoteDescriptor.Id, 
                featureUri, context?.ToRequestMetadata(), 
                cancellationToken
            );
        }


        /// <inheritdoc/>
        protected override Task<string> Invoke(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            var client = Proxy.GetClient();
            return client.Extensions.InvokeExtensionAsync(
                Proxy.RemoteDescriptor.Id, 
                operationId, 
                argument, 
                context?.ToRequestMetadata(), 
                cancellationToken
            );
        }

    }
}
