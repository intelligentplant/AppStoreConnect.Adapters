using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Routing {

    /// <summary>
    /// A service that registers routes with an <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    internal interface IRouteProvider {

        /// <summary>
        /// Registers the provider's routes with the <see cref="IEndpointRouteBuilder"/>.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IEndpointRouteBuilder"/>.
        /// </param>
        public static abstract void Register(IEndpointRouteBuilder builder);

    }
}
