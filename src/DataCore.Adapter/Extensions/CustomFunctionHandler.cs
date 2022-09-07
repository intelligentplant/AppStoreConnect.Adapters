#pragma warning disable CS0618 // Type or member is obsolete
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


    /// <summary>
    /// A delegate that processes a custom function request.
    /// </summary>
    /// <typeparam name="TRequest">
    ///   The request type of the custom function.
    /// </typeparam>
    /// <typeparam name="TResponse">
    ///   The response type of the custom function.
    /// </typeparam>
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
    ///   A <see cref="Task{TResult}"/> that will return the result of the custom function call.
    /// </returns>
    public delegate Task<TResponse> CustomFunctionHandler<TRequest, TResponse>(
        IAdapterCallContext context, 
        TRequest request, 
        CancellationToken cancellationToken
    );


    /// <summary>
    /// A delegate that processes a custom function request with no request object.
    /// </summary>
    /// <typeparam name="TResponse">
    ///   The response type of the custom function.
    /// </typeparam>
    /// <param name="context">
    ///   The <see cref="IAdapterCallContext"/> for the caller.
    /// </param>
    /// <param name="cancellationToken">
    ///   The cancellation token for the operation.
    /// </param>
    /// <returns>
    ///   A <see cref="Task{TResult}"/> that will return the result of the custom function call.
    /// </returns>
    public delegate Task<TResponse> CustomFunctionHandler<TResponse>(
        IAdapterCallContext context,
        CancellationToken cancellationToken
    );

}
#pragma warning restore CS0618 // Type or member is obsolete
