using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.Proxy;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Extensions {

    /// <summary>
    /// Allows communication with remote <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    public class AdapterExtensionFeatureImpl : ExtensionFeatureProxyBase<SignalRAdapterProxy> {

        /// <summary>
        /// Creates a new <see cref="AdapterExtensionFeatureImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="proxy"/> is <see langword="null"/>.
        /// </exception>
        public AdapterExtensionFeatureImpl(SignalRAdapterProxy proxy) 
            : base(proxy) { }


        /// <inheritdoc/>
        protected override Task<FeatureDescriptor?> GetDescriptorFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            var client = Proxy.GetClient();
            return client.Extensions.GetDescriptorAsync(Proxy.RemoteDescriptor.Id, featureUri!, cancellationToken)!;
        }


        /// <inheritdoc/>
        protected override Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperationsFromRemoteAdapter(
            IAdapterCallContext context, 
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            var client = Proxy.GetClient();
            return client.Extensions.GetOperationsAsync(Proxy.RemoteDescriptor.Id, featureUri!, cancellationToken);
        }


        /// <inheritdoc/>
        protected override Task<string> InvokeInternal(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            var client = Proxy.GetClient();
            return client.Extensions.InvokeExtensionAsync(Proxy.RemoteDescriptor.Id, operationId, argument, cancellationToken)!;
        }


        /// <inheritdoc/>
        protected override Task<ChannelReader<string>> StreamInternal(IAdapterCallContext context, Uri operationId, string argument, CancellationToken cancellationToken) {
            var client = Proxy.GetClient();
            return client.Extensions.InvokeStreamingExtensionAsync(Proxy.RemoteDescriptor.Id, operationId, argument, cancellationToken)!;
        }


        /// <inheritdoc/>
        protected override Task<ChannelReader<string>> DuplexStreamInternal(IAdapterCallContext context, Uri operationId, ChannelReader<string> channel, CancellationToken cancellationToken) {
            var client = Proxy.GetClient();
            return client.Extensions.InvokeDuplexStreamingExtensionAsync(Proxy.RemoteDescriptor.Id, operationId, channel, cancellationToken);
        }

    }
}
