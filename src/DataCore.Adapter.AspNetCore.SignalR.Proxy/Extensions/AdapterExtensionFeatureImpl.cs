using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Extensions {

    /// <summary>
    /// Allows communication with remote <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    public class AdapterExtensionFeatureImpl : AdapterExtensionFeature {

        /// <summary>
        /// The owning proxy.
        /// </summary>
        private readonly SignalRAdapterProxy _proxy;


        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeatureImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="proxy"/> is <see langword="null"/>.
        /// </exception>
        public AdapterExtensionFeatureImpl(SignalRAdapterProxy proxy) {
            _proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
        }


        /// <inheritdoc/>
        protected override Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperations(
            IAdapterCallContext context, 
            Uri featureUri, 
            CancellationToken cancellationToken
        ) {
            var client = _proxy.GetClient();
            return client.Extensions.GetOperationsAsync(_proxy.RemoteDescriptor.Id, featureUri, cancellationToken);
        }


        /// <inheritdoc/>
        protected override Task<string> Invoke(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            var client = _proxy.GetClient();
            return client.Extensions.InvokeExtensionAsync(_proxy.RemoteDescriptor.Id, operationId, argument, cancellationToken);
        }


        /// <inheritdoc/>
        protected override Task<ChannelReader<string>> Stream(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            var client = _proxy.GetClient();
            return client.Extensions.InvokeStreamingExtensionAsync(_proxy.RemoteDescriptor.Id, operationId, argument, cancellationToken);
        }


        /// <inheritdoc/>
        protected override Task<ChannelReader<string>> DuplexStream(IAdapterCallContext context, Uri operationId, ChannelReader<string> channel, CancellationToken cancellationToken) {
            var client = _proxy.GetClient();
            return client.Extensions.InvokeDuplexStreamingExtensionAsync(_proxy.RemoteDescriptor.Id, operationId, channel, cancellationToken);
        }

    }
}
