using System.Security;

using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using MiniValidation;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class AdapterRoutes : IRouteProvider {

        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapGet("/", FindAdaptersGetAsync)
                .Produces<IAsyncEnumerable<AdapterDescriptor>>()
                .ProducesValidationProblem();

            builder.MapPost("/", FindAdaptersPostAsync)
                .Produces<IAsyncEnumerable<AdapterDescriptor>>()
                .ProducesValidationProblem();

            builder.MapGet("/{adapterId}", GetAdapterAsync)
                .Produces<AdapterDescriptorExtended>()
                .ProducesDefaultErrors();

            builder.MapGet("/{adapterId}/health-status", CheckAdapterHealthAsync)
                .Produces<HealthCheckResult>()
                .ProducesDefaultErrors();
        }


        private static async Task<IResult> FindAdaptersGetAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string? id = null,
            string? name = null,
            string? description = null,
            string[]? feature = null,
            int pageSize = 10,
            int page = 1,

            CancellationToken cancellationToken = default
        ) {
            return await FindAdaptersPostAsync(context, adapterAccessor, new FindAdaptersRequest() {
                Description = description,
                Features = feature,
                Id = id,
                Name = name,
                Page = page,
                PageSize = pageSize
            }, cancellationToken).ConfigureAwait(false);
        }


        private static async Task<IResult> FindAdaptersPostAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            FindAdaptersRequest request, 
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await MiniValidator.TryValidateAsync(request, true).ConfigureAwait(false);
            if (!resolverResult.IsValid) {
                return Results.ValidationProblem(resolverResult.Errors);
            }

            var callContext = new HttpAdapterCallContext(context);
            if (request.PageSize > 100) {
                // Don't allow arbitrarily large queries!
                request.PageSize = 100;
            }

            var adapters = adapterAccessor.FindAdapters(callContext, request, cancellationToken);
            return Results.Ok(adapters.Transform(adapter => adapter.Descriptor, cancellationToken));
        }


        private static async Task<IResult> GetAdapterAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAsync<IHealthCheck>(context, adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(resolverResult.Adapter.CreateExtendedAdapterDescriptor());
        }


        private static async Task<IResult> CheckAdapterHealthAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAsync<IHealthCheck>(context, adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.CheckHealthAsync(resolverResult.CallContext, cancellationToken).ConfigureAwait(false));
        }

    }
}
