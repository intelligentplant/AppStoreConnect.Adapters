using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {

    [Obsolete(ExtensionFeatureConstants.ObsoleteMessage, ExtensionFeatureConstants.ObsoleteError)]
    internal class ExtensionRoutes : IRouteProvider {

        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapGet("/{adapterId}", GetAvailableExtensionsAsync)
                .Produces<IEnumerable<string>>()
                .ProducesDefaultErrors();

            builder.MapGet("/{adapterId}/descriptor", GetDescriptorAsync)
                .Produces<Common.FeatureDescriptor>()
                .ProducesDefaultErrors();

            builder.MapGet("/{adapterId}/operations", GetAvailableOperationsAsync)
                .Produces<IEnumerable<ExtensionFeatureOperationDescriptor>>()
                .ProducesDefaultErrors();

            builder.MapPost("/{adapterId}/operations/invoke", InvokeOperationAsync)
                .Produces<InvocationResponse>()
                .ProducesDefaultErrors();
        }


        private static async Task<IResult> GetAvailableExtensionsAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId, 
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAsync(context, adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            var descriptor = resolverResult.Adapter.CreateExtendedAdapterDescriptor();
            return Results.Ok(descriptor.Extensions);
        }


        private static async Task<IResult> GetDescriptorAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            Uri id,
            CancellationToken cancellationToken = default
        ) {
            id = id?.EnsurePathHasTrailingSlash()!;

            if (id == null || !id.IsAbsoluteUri) {
                return Results.Problem(statusCode: 400, detail: string.Format(System.Globalization.CultureInfo.CurrentUICulture, Resources.Error_UnsupportedInterface, id)); // 400
            }

            var resolverResult = await Utils.ResolveAdapterAsync<IAdapterExtensionFeature>(context, adapterAccessor, adapterId, id, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.GetDescriptor(resolverResult.CallContext, id, cancellationToken).ConfigureAwait(false));
        }


        private static async Task<IResult> GetAvailableOperationsAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            Uri id,
            CancellationToken cancellationToken = default
        ) {
            id = id?.EnsurePathHasTrailingSlash()!;

            if (id == null || !id.IsAbsoluteUri) {
                return Results.Problem(statusCode: 400, detail: string.Format(System.Globalization.CultureInfo.CurrentUICulture, Resources.Error_UnsupportedInterface, id)); // 400
            }

            var resolverResult = await Utils.ResolveAdapterAsync<IAdapterExtensionFeature>(context, adapterAccessor, adapterId, id, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            var ops = await resolverResult.Feature.GetOperations(resolverResult.CallContext, id, cancellationToken).ConfigureAwait(false);
            
            // Note that we filter out any non-invocation operations here!
            return Results.Ok(ops?.Where(x => x != null && x.OperationType == ExtensionFeatureOperationType.Invoke).ToArray() ?? Array.Empty<ExtensionFeatureOperationDescriptor>()); // 200
        }


        private static async Task<IResult> InvokeOperationAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            CompatibilityInvocationRequest request,
            CancellationToken cancellationToken = default
        ) {
            var validationError = await Utils.ValidateRequestAsync(request, false).ConfigureAwait(false);
            if (validationError != null) {
                return validationError;
            }

            var requestActual = request.ToAdapterRequest();
            var id = requestActual.OperationId.EnsurePathHasTrailingSlash();
            if (!AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(id, out var featureUri, out var error)) {
                return Results.Problem(statusCode: 400, detail: error); // 400
            }

            var resolverResult = await Utils.ResolveAdapterAsync<IAdapterExtensionFeature>(context, adapterAccessor, adapterId, featureUri, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.Invoke(resolverResult.CallContext, requestActual, cancellationToken).ConfigureAwait(false));
        }


        /// <summary>
        /// Compatibility model to work around https://github.com/DamianEdwards/MiniValidation/issues/44
        /// </summary>
        internal class CompatibilityInvocationRequest : Common.AdapterRequest {

            [System.ComponentModel.DataAnnotations.Required]
            public string OperationId { get; set; } = default!;

            private Uri? _uri;

            [System.ComponentModel.DataAnnotations.Required]
            public Common.Variant[] Arguments { get; set; } = Array.Empty<Common.Variant>();


            protected override IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext) {
                foreach (var item in base.Validate(validationContext)) {
                    yield return item;
                }

                if (OperationId != null) {
                    if (!Uri.TryCreate(OperationId, UriKind.RelativeOrAbsolute, out _uri)) {
                        yield return new System.ComponentModel.DataAnnotations.ValidationResult(SharedResources.Error_InvalidUri, new[] { nameof(OperationId) });
                    }
                }
            }


            public InvocationRequest ToAdapterRequest() {
                return new InvocationRequest() {
                    OperationId = _uri ??= new Uri(OperationId, UriKind.RelativeOrAbsolute),
                    Arguments = Arguments,
                    Properties = Properties
                };
            }

        }


    }

}
