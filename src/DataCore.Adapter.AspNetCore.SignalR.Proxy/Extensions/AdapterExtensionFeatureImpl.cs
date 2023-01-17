using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.Proxy;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Extensions {

    /// <summary>
    /// Allows communication with remote <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
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
        protected override async Task<FeatureDescriptor?> GetDescriptorFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            var client = Proxy.GetClient();
            return await client.Extensions.GetDescriptorAsync(Proxy.RemoteDescriptor.Id, featureUri!, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperationsFromRemoteAdapter(
            IAdapterCallContext context, 
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            var client = Proxy.GetClient();
            return await client.Extensions.GetOperationsAsync(Proxy.RemoteDescriptor.Id, featureUri!, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async Task<InvocationResponse> InvokeInternal(IAdapterCallContext context, InvocationRequest request, CancellationToken cancellationToken) {
            var client = Proxy.GetClient();
            return await client.Extensions.InvokeExtensionAsync(Proxy.RemoteDescriptor.Id, request, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<InvocationResponse> StreamInternal(
            IAdapterCallContext context, 
            InvocationRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = Proxy.GetClient();
            await foreach (var item in client.Extensions.InvokeStreamingExtensionAsync(Proxy.RemoteDescriptor.Id, request, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<InvocationResponse> DuplexStreamInternal(
            IAdapterCallContext context, 
            DuplexStreamInvocationRequest request, 
            IAsyncEnumerable<InvocationStreamItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            var client = Proxy.GetClient();
            await foreach (var item in client.Extensions.InvokeDuplexStreamingExtensionAsync(Proxy.RemoteDescriptor.Id, request, channel, cancellationToken)) {
                yield return item;
            }
        }

    }
}
