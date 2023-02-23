using System.Security;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder {

    /// <summary>
    /// Extensions for <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    public static class AdapterMinimalApiEndpointRouteBuilderExtensions {

        /// <summary>
        /// Maps adapter API routes.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IEndpointRouteBuilder"/>.
        /// </param>
        /// <returns>
        ///   The base <see cref="IEndpointRouteBuilder"/> for the adapter API routes.
        /// </returns>
        public static IEndpointRouteBuilder MapDataCoreAdapterApiRoutes(this IEndpointRouteBuilder builder) {
            var api = builder.MapGroup("/api/app-store-connect");
            
            api.AddEndpointFilter(async (context, next) => {
                try {
                    return await next(context);
                }
                catch (SecurityException) {
                    return Results.Problem(statusCode: StatusCodes.Status403Forbidden);
                }
                catch (ArgumentException e) {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: e.Message);
                }
                catch (InvalidOperationException e) {
                    return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: e.Message);
                }
                catch {
                    throw;
                }
            });

            var v2api = api.MapGroup("/v2.0");

            DataCore.Adapter.AspNetCore.Routing.V2.AdapterRoutes.Register(v2api.MapGroup("/adapters"));
            DataCore.Adapter.AspNetCore.Routing.V2.AssetModelRoutes.Register(v2api.MapGroup("/asset-model"));
            DataCore.Adapter.AspNetCore.Routing.V2.CustomFunctionRoutes.Register(v2api.MapGroup("/custom-functions"));
            DataCore.Adapter.AspNetCore.Routing.V2.EventRoutes.Register(v2api.MapGroup("/events"));
#pragma warning disable CS0618 // Type or member is obsolete
            DataCore.Adapter.AspNetCore.Routing.V2.ExtensionRoutes.Register(v2api.MapGroup("/extensions"));
#pragma warning restore CS0618 // Type or member is obsolete
            DataCore.Adapter.AspNetCore.Routing.V2.HostInfoRoutes.Register(v2api.MapGroup("/host-info"));
            DataCore.Adapter.AspNetCore.Routing.V2.TagAnnotationRoutes.Register(v2api.MapGroup("/tag-annotations"));
            DataCore.Adapter.AspNetCore.Routing.V2.TagRoutes.Register(v2api.MapGroup("/tags"));
            DataCore.Adapter.AspNetCore.Routing.V2.TagValueRoutes.Register(v2api.MapGroup("/tag-values"));

            return api;
        }

    }
}
