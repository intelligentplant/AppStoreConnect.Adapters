using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.Proxy;

namespace DataCore.Adapter.Http.Proxy.Extensions {
    /// <summary>
    /// Allows communication with remote <see cref="IAdapterExtensionFeature"/> implementations.
    /// </summary>
    public class AdapterExtensionFeatureImpl : ExtensionFeatureProxyBase<HttpAdapterProxy, HttpAdapterProxyOptions> {

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
        protected override async Task<FeatureDescriptor?> GetDescriptorFromRemoteAdapter(
            IAdapterCallContext context,
            Uri? featureUri,
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context);

            var client = Proxy.GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) { 
                return await client.Extensions.GetDescriptorAsync(
                    Proxy.RemoteDescriptor.Id,
                    featureUri!,
                    context?.ToRequestMetadata(),
                    ctSource.Token
                ).ConfigureAwait(false);
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
                return await client.Extensions.GetOperationsAsync(
                    Proxy.RemoteDescriptor.Id,
                    featureUri!,
                    context?.ToRequestMetadata(),
                    ctSource.Token
                ).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        protected override async Task<InvocationResponse> InvokeCore(IAdapterCallContext context, InvocationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = Proxy.GetClient();

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                return await client.Extensions.InvokeExtensionAsync(
                    Proxy.RemoteDescriptor.Id,
                    request,
                    context?.ToRequestMetadata(),
                    ctSource.Token
                ).ConfigureAwait(false);
            }
        }

    }
}
