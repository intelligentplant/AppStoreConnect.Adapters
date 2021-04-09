using System;
using System.Collections.Generic;
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
    public class AdapterExtensionFeatureImpl : ExtensionFeatureProxyBase<SignalRAdapterProxy, SignalRAdapterProxyOptions> {

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
            : base(proxy, proxy.Encoders) { }


        /// <inheritdoc/>
        protected override Task<FeatureDescriptor?> GetDescriptorFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context);
            var client = Proxy.GetClient();
            return client.Extensions.GetDescriptorAsync(Proxy.RemoteDescriptor.Id, featureUri!, cancellationToken)!;
        }


        /// <inheritdoc/>
        protected override Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperationsFromRemoteAdapter(
            IAdapterCallContext context, 
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context);
            var client = Proxy.GetClient();
            return client.Extensions.GetOperationsAsync(Proxy.RemoteDescriptor.Id, featureUri!, cancellationToken);
        }


        /// <inheritdoc/>
        protected override Task<InvocationResponse> InvokeInternal(IAdapterCallContext context, InvocationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context);
            var client = Proxy.GetClient();
            return client.Extensions.InvokeExtensionAsync(Proxy.RemoteDescriptor.Id, request, cancellationToken)!;
        }


        /// <inheritdoc/>
        protected override IAsyncEnumerable<InvocationResponse> StreamInternal(IAdapterCallContext context, InvocationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context);
            var client = Proxy.GetClient();
            return client.Extensions.InvokeStreamingExtensionAsync(Proxy.RemoteDescriptor.Id, request, cancellationToken)!;
        }


        /// <inheritdoc/>
        protected override IAsyncEnumerable<InvocationResponse> DuplexStreamInternal(IAdapterCallContext context, DuplexStreamInvocationRequest request, IAsyncEnumerable<InvocationStreamItem> channel, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, channel);
            var client = Proxy.GetClient();
            return client.Extensions.InvokeDuplexStreamingExtensionAsync(Proxy.RemoteDescriptor.Id, request, channel, cancellationToken);
        }

    }
}
