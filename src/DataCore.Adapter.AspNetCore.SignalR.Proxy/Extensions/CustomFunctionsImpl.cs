using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy.Extensions {

    /// <summary>
    /// Implements <see cref="ICustomFunctions"/>.
    /// </summary>
    internal class CustomFunctionsImpl : ProxyAdapterFeature, ICustomFunctions {

        /// <summary>
        /// Creates a new <see cref="CustomFunctionsImpl"/> object.
        /// </summary>
        /// <param name="proxy">
        ///   The owning proxy.
        /// </param>
        public CustomFunctionsImpl(SignalRAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async Task<IEnumerable<CustomFunctionDescriptor>> GetFunctionsAsync(
            IAdapterCallContext context, 
            GetCustomFunctionsRequest request, 
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            return await client.CustomFunctions.GetFunctionsAsync(AdapterId, request, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task<CustomFunctionDescriptorExtended?> GetFunctionAsync(
            IAdapterCallContext context, 
            GetCustomFunctionRequest request, 
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            return await client.CustomFunctions.GetFunctionAsync(AdapterId, request, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public async Task<CustomFunctionInvocationResponse> InvokeFunctionAsync(
            IAdapterCallContext context, 
            CustomFunctionInvocationRequest request, 
            CancellationToken cancellationToken
        ) {
            var client = GetClient();
            return await client.CustomFunctions.InvokeFunctionAsync(AdapterId, request, cancellationToken).ConfigureAwait(false);
        }

    }
}
