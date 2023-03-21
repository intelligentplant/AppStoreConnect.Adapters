using System.Globalization;

using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class CustomFunctionRoutes : IRouteProvider {
        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapGet("/{adapterId}", GetCustomFunctionsGetAsync)
                .Produces<IEnumerable<CustomFunctionDescriptor>>()
                .ProducesDefaultErrors();

            builder.MapPost("/{adapterId}", GetCustomFunctionsPostAsync)
                .Produces<IEnumerable<CustomFunctionDescriptor>>()
                .ProducesDefaultErrors();

            builder.MapGet("/{adapterId}/details", GetCustomFunctionGetAsync)
                .Produces<CustomFunctionDescriptorExtended>()
                .ProducesDefaultErrors();

            builder.MapPost("/{adapterId}/details", GetCustomFunctionPostAsync)
                .Produces<CustomFunctionDescriptorExtended>()
                .ProducesDefaultErrors();

            builder.MapPost("/{adapterId}/invoke", InvokeCustomFunctionAsync)
                .Produces<CustomFunctionInvocationResponse>()
                .ProducesDefaultErrors();
        }


        private static async Task<IResult> GetCustomFunctionsGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string? id = null, 
            string? name = null, 
            string? description = null, 
            int pageSize = 10, 
            int page = 1,
            CancellationToken cancellationToken = default
        ) {
            return await GetCustomFunctionsPostAsync(context, adapterAccessor, adapterId, new GetCustomFunctionsRequest() { 
                Description = description,
                Id = id,
                Name = name,
                Page = page,
                PageSize = pageSize
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> GetCustomFunctionsPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            GetCustomFunctionsRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ICustomFunctions>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            
            return Results.Ok(await resolverResult.Feature.GetFunctionsAsync(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }


        private static async Task<IResult> GetCustomFunctionGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            Uri id,
            CancellationToken cancellationToken = default
        ) {
            return await GetCustomFunctionPostAsync(context, adapterAccessor, adapterId, new GetCustomFunctionRequest() { 
                Id = id
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> GetCustomFunctionPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            GetCustomFunctionRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ICustomFunctions>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.GetFunctionAsync(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }


        private static async Task<IResult> InvokeCustomFunctionAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            IOptions<JsonOptions> jsonOptions,
            string adapterId,
            CustomFunctionInvocationRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ICustomFunctions>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            var function = await resolverResult.Feature.GetFunctionAsync(resolverResult.CallContext, new GetCustomFunctionRequest() {
                Id = request.Id
            }, cancellationToken).ConfigureAwait(false);

            if (function == null) {
                return Results.Problem(statusCode: 400, detail: string.Format(CultureInfo.CurrentCulture, AbstractionsResources.Error_UnableToResolveCustomFunction, request.Id));
            }

            if (!request.TryValidateBody(function, jsonOptions.Value?.SerializerOptions, out var validationResults)) {
                return Results.Problem(statusCode: 400, detail: SharedResources.Error_InvalidRequestBody, extensions: new Dictionary<string, object?>() {
                    ["errors"] = validationResults
                });
            }

            return Results.Ok(await resolverResult.Feature.InvokeFunctionAsync(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }

    }
}
