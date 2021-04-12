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
            Proxy.ValidateInvocation(context);
            var client = Proxy.GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                return await client.Extensions.GetDescriptorAsync(Proxy.RemoteDescriptor.Id, featureUri!, ctSource.Token).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async Task<IEnumerable<ExtensionFeatureOperationDescriptor>> GetOperationsFromRemoteAdapter(
            IAdapterCallContext context, 
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context);
            var client = Proxy.GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                return await client.Extensions.GetOperationsAsync(Proxy.RemoteDescriptor.Id, featureUri!, ctSource.Token).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async Task<InvocationResponse> InvokeInternal(IAdapterCallContext context, InvocationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);
            var client = Proxy.GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                return await client.Extensions.InvokeExtensionAsync(Proxy.RemoteDescriptor.Id, request, ctSource.Token).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<InvocationResponse> StreamInternal(
            IAdapterCallContext context, 
            InvocationRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);
            var client = Proxy.GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach (var item in client.Extensions.InvokeStreamingExtensionAsync(Proxy.RemoteDescriptor.Id, request, ctSource.Token).ConfigureAwait(false)) {
                    yield return item;
                }
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
            Proxy.ValidateInvocation(context, request, channel);
            var client = Proxy.GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                await foreach (var item in client.Extensions.InvokeDuplexStreamingExtensionAsync(Proxy.RemoteDescriptor.Id, request, channel, ctSource.Token)) {
                    yield return item;
                }
            }
        }

    }
}
