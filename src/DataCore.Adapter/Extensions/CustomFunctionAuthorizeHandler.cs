using System.Threading;
using System.Threading.Tasks;

namespace DataCore.Adapter.Extensions {

    /// <summary>
    /// A delegate that authorizes access to a custom function.
    /// </summary>
    /// <param name="context">
    ///   The <see cref="IAdapterCallContext"/> for the caller.
    /// </param>
    /// <param name="cancellationToken">
    ///   The cancellation token for the operation.
    /// </param>
    /// <returns>
    ///   A <see cref="ValueTask{TResult}"/> that returns <see langword="true"/> if the caller is 
    ///   authorized to invoke the custom function, or <see langword="false"/> otherwise.
    /// </returns>
    public delegate ValueTask<bool> CustomFunctionAuthorizeHandler(IAdapterCallContext context, CancellationToken cancellationToken);

}
