using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Wrapper for <see cref="ICustomFunctions"/>.
    /// </summary>
    internal class CustomFunctionsWrapper : AdapterFeatureWrapper<ICustomFunctions>, ICustomFunctions {

        /// <summary>
        /// Creates a new <see cref="CustomFunctionsWrapper"/> instance.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter that the feature is being assigned to.
        /// </param>
        /// <param name="innerFeature">
        ///   The feature implementaton to wrap.
        /// </param>
        internal CustomFunctionsWrapper(AdapterCore adapter, ICustomFunctions innerFeature)
            : base(adapter, innerFeature) { }


        /// <inheritdoc/>
        Task<IEnumerable<CustomFunctionDescriptor>> ICustomFunctions.GetFunctionsAsync(IAdapterCallContext context, GetCustomFunctionsRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.GetFunctionsAsync, cancellationToken);
        }


        /// <inheritdoc/>
        Task<CustomFunctionDescriptorExtended?> ICustomFunctions.GetFunctionAsync(IAdapterCallContext context, GetCustomFunctionRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.GetFunctionAsync, cancellationToken);
        }


        /// <inheritdoc/>
        Task<CustomFunctionInvocationResponse> ICustomFunctions.InvokeFunctionAsync(IAdapterCallContext context, CustomFunctionInvocationRequest request, CancellationToken cancellationToken) {
            return InvokeAsync(context, request, InnerFeature.InvokeFunctionAsync, cancellationToken);
        }

    }

}
