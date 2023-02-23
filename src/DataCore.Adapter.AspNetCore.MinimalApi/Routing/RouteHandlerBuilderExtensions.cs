using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace DataCore.Adapter.AspNetCore.Routing {
    internal static class RouteHandlerBuilderExtensions {

        internal static RouteHandlerBuilder ProducesDefaultErrors(this RouteHandlerBuilder builder) {
            builder
                .ProducesValidationProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status403Forbidden);

            return builder;
        }

    }
}
