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
        ///   The base <see cref="IEndpointConventionBuilder"/> for the adapter API routes.
        /// </returns>
        public static IEndpointConventionBuilder MapDataCoreAdapterApiRoutes(this IEndpointRouteBuilder builder) => builder.MapDataCoreAdapterApiRoutes(null);


        /// <summary>
        /// Maps adapter API routes.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="IEndpointRouteBuilder"/>.
        /// </param>
        /// <param name="prefix">
        ///   The route prefix for the API routes. Specify <see langword="null"/> to use no 
        ///   prefix.
        /// </param>
        /// <returns>
        ///   The base <see cref="IEndpointConventionBuilder"/> for the adapter API routes.
        /// </returns>
        public static IEndpointConventionBuilder MapDataCoreAdapterApiRoutes(this IEndpointRouteBuilder builder, PathString? prefix) {
            var versionedApiRouteBuilder = builder.NewVersionedApi();

            // Base for all versioned API routes.
            var apiBasePath = prefix == null
                ? "/api/app-store-connect"
                : prefix.Value.Add(new PathString("/api/app-store-connect")).ToString();

            var api = versionedApiRouteBuilder.MapGroup(string.Concat(apiBasePath, "/v{version:apiVersion}")).WithOpenApi();

            // Add common error handling.
            api.AddEndpointFilter(async (context, next) => {
                try {
                    return await next(context).ConfigureAwait(false);
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

            // Base for the v2.0 API
            var v2api = api.MapGroup("/").HasApiVersion(2, 0);

            DataCore.Adapter.AspNetCore.Routing.V2.AdapterRoutes.Register(v2api.MapGroup("/adapters")
                .WithGroupName("Adapters"));
            
            DataCore.Adapter.AspNetCore.Routing.V2.AssetModelRoutes.Register(v2api.MapGroup("/asset-model")
                .WithGroupName("Asset Model"));
            
            DataCore.Adapter.AspNetCore.Routing.V2.CustomFunctionRoutes.Register(v2api.MapGroup("/custom-functions")
                .WithGroupName("Custom Functions"));
            
            DataCore.Adapter.AspNetCore.Routing.V2.EventRoutes.Register(v2api.MapGroup("/events")
                .WithGroupName("Events"));

#pragma warning disable CS0618 // Type or member is obsolete
            DataCore.Adapter.AspNetCore.Routing.V2.ExtensionRoutes.Register(v2api.MapGroup("/extensions")
                .WithGroupName("Extension Features"));
#pragma warning restore CS0618 // Type or member is obsolete
            
            DataCore.Adapter.AspNetCore.Routing.V2.HostInfoRoutes.Register(v2api.MapGroup("/host-info")
                .WithGroupName("Host Information"));
            
            DataCore.Adapter.AspNetCore.Routing.V2.TagAnnotationRoutes.Register(v2api.MapGroup("/tag-annotations")
                .WithGroupName("Annotations"));
            
            DataCore.Adapter.AspNetCore.Routing.V2.TagRoutes.Register(v2api.MapGroup("/tags")
                .WithGroupName("Tags"));
            
            DataCore.Adapter.AspNetCore.Routing.V2.TagValueRoutes.Register(v2api.MapGroup("/tag-values")
                .WithGroupName("Tag Values"));

            return api;
        }

    }
}
