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
            return await GetCustomFunctionPostAsync(context, adapterAccessor, adapterId, new CompatibilityGetCustomFunctionRequest() { 
                Id = id.ToString()
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> GetCustomFunctionPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            CompatibilityGetCustomFunctionRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ICustomFunctions>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            var requestActual = request.ToAdapterRequest();

            return Results.Ok(await resolverResult.Feature.GetFunctionAsync(resolverResult.CallContext, requestActual, cancellationToken).ConfigureAwait(false));
        }


        private static async Task<IResult> InvokeCustomFunctionAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            IOptions<JsonOptions> jsonOptions,
            string adapterId,
            CompatibilityCustomFunctionInvocationRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<ICustomFunctions>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            var requestActual = request.ToAdapterRequest();

            var function = await resolverResult.Feature.GetFunctionAsync(resolverResult.CallContext, new GetCustomFunctionRequest() {
                Id = requestActual.Id
            }, cancellationToken).ConfigureAwait(false);

            if (function == null) {
                return Results.Problem(statusCode: 400, detail: string.Format(CultureInfo.CurrentCulture, AbstractionsResources.Error_UnableToResolveCustomFunction, request.Id));
            }

            if (!requestActual.TryValidateBody(function, jsonOptions.Value?.SerializerOptions, out var validationResults)) {
                return Results.Problem(statusCode: 400, detail: SharedResources.Error_InvalidRequestBody, extensions: new Dictionary<string, object?>() {
                    ["errors"] = validationResults
                });
            }

            return Results.Ok(await resolverResult.Feature.InvokeFunctionAsync(resolverResult.CallContext, requestActual, cancellationToken).ConfigureAwait(false));
        }


        /// <summary>
        /// Compatibility model to work around https://github.com/DamianEdwards/MiniValidation/issues/44
        /// </summary>
        internal class CompatibilityGetCustomFunctionRequest : Common.AdapterRequest {

            [System.ComponentModel.DataAnnotations.Required]
            public string Id { get; set; } = default!;

            private Uri? _uri;


            protected override IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext) {
                foreach (var item in base.Validate(validationContext)) {
                    yield return item;
                }

                if (Id != null) {
                    if (!Uri.TryCreate(Id, UriKind.RelativeOrAbsolute, out _uri)) {
                        yield return new System.ComponentModel.DataAnnotations.ValidationResult(SharedResources.Error_InvalidUri, new[] { nameof(Id) });
                    }
                }
            }


            public GetCustomFunctionRequest ToAdapterRequest() {
                return new GetCustomFunctionRequest() {
                    Id = _uri ??= new Uri(Id, UriKind.RelativeOrAbsolute),
                    Properties = Properties
                };
            }

        }


        /// <summary>
        /// Compatibility model to work around https://github.com/DamianEdwards/MiniValidation/issues/44
        /// </summary>
        internal class CompatibilityCustomFunctionInvocationRequest : Common.AdapterRequest {

            [System.ComponentModel.DataAnnotations.Required]
            public string Id { get; set; } = default!;

            private Uri? _uri;

            public System.Text.Json.JsonElement? Body { get; set; }


            protected override IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext) {
                foreach (var item in base.Validate(validationContext)) {
                    yield return item;
                }

                if (Id != null) {
                    if (!Uri.TryCreate(Id, UriKind.RelativeOrAbsolute, out _uri)) {
                        yield return new System.ComponentModel.DataAnnotations.ValidationResult(SharedResources.Error_InvalidUri, new[] { nameof(Id) });
                    }
                }
            }


            public CustomFunctionInvocationRequest ToAdapterRequest() {
                return new CustomFunctionInvocationRequest() {
                    Id = _uri ??= new Uri(Id, UriKind.RelativeOrAbsolute),
                    Body = Body,
                    Properties = Properties
                };
            }

        }

    }
}
