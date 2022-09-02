using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A delegate that processes a custom function request.
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
    public delegate Task<CustomFunctionInvocationResponse> CustomFunctionHandler(
        IAdapterCallContext context, 
        CustomFunctionInvocationRequest request, 
        CancellationToken cancellationToken
    );

}
