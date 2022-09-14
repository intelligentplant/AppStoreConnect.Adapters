using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// Feature for invoking custom functions on an adapter.
    /// </summary>
    [AdapterFeature(
        WellKnownFeatures.Extensions.CustomFunctions,
        ResourceType = typeof(AbstractionsResources),
        Name = nameof(AbstractionsResources.DisplayName_CustomFunctions),
        Description = nameof(AbstractionsResources.Description_CustomFunctions)
    )]
    public interface ICustomFunctions : IAdapterFeature {

        /// <summary>
        /// Gets the available custom functions for the adapter.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="CustomFunctionDescriptor"/> 
        ///   for each custom function defined by the adapter.
        /// </returns>
        Task<IEnumerable<CustomFunctionDescriptor>> GetFunctionsAsync(
            IAdapterCallContext context, 
            GetCustomFunctionsRequest request,
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Gets the extended descriptor for the specified custom function.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a <see cref="CustomFunctionDescriptorExtended"/> 
        ///   describing the function.
        /// </returns>
        Task<CustomFunctionDescriptorExtended?> GetFunctionAsync(
            IAdapterCallContext context, 
            GetCustomFunctionRequest request, 
            CancellationToken cancellationToken
        );


        /// <summary>
        /// Invokes a custom function.
        /// </summary>
        /// <param name="context">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the invocation response.
        /// </returns>
        Task<CustomFunctionInvocationResponse> InvokeFunctionAsync(
            IAdapterCallContext context, 
            CustomFunctionInvocationRequest request, 
            CancellationToken cancellationToken
        );

    }
}
