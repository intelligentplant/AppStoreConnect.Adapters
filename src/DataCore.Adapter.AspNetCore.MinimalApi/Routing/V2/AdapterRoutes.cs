using System.Security;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using MiniValidation;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class AdapterRoutes : IRouteProvider {

        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapGet("/", FindAdaptersGetAsync);
            builder.MapPost("/", FindAdaptersPostAsync);
            builder.MapGet("/{adapterId}", GetAdapterAsync);
            builder.MapGet("/{adapterId}/health-status", CheckAdapterHealthAsync);
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
            var callContext = new HttpAdapterCallContext(context);
            var adapter = await adapterAccessor.GetAdapter(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return Results.NotFound();
            }

            return Results.Ok(adapter.CreateExtendedAdapterDescriptor());
        }


        private static async Task<IResult> CheckAdapterHealthAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            CancellationToken cancellationToken = default
        ) {
            var callContext = new HttpAdapterCallContext(context);
            var resolvedFeature = await adapterAccessor.GetAdapterAndFeature<IHealthCheck>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return Results.BadRequest(string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)); // 400
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return Results.BadRequest(string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, nameof(IHealthCheck))); // 400
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return Results.Forbid(); // 403
            }
            var feature = resolvedFeature.Feature;

            try {
                return Results.Ok(await feature.CheckHealthAsync(callContext, cancellationToken).ConfigureAwait(false)); // 200
            }
            catch (SecurityException) {
                return Results.Forbid(); // 403
            }
        }

    }
}
