using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.RealTimeData;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class TagAnnotationRoutes : IRouteProvider {

        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapPost("/{adapterId}", ReadAnnotationsAsync);
            builder.MapGet("/{adapterId}/{tagId}/{annotationId}", ReadAnnotationAsync);
            builder.MapPost("/{adapterId}/{tagId}/create", CreateAnnotationAsync);
            builder.MapPut("/{adapterId}/{tagId}/{annotationId}", UpdateAnnotationAsync);
            builder.MapDelete("/{adapterId}/{tagId}/{annotationId}", DeleteAnnotationAsync);
        }


        private static async Task<IResult> ReadAnnotationsAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadAnnotationsRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(resolverResult.Feature.ReadAnnotations(resolverResult.CallContext, request, cancellationToken));
        }


        private static async Task<IResult> ReadAnnotationAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string tagId,
            string annotationId,
            CancellationToken cancellationToken = default
        ) {
            var request = new ReadAnnotationRequest() {
                AnnotationId = annotationId,
                Tag = tagId,
            };

            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(await resolverResult.Feature.ReadAnnotation(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }


        private static async Task<IResult> CreateAnnotationAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string tagId,
            TagValueAnnotation annotation,
            CancellationToken cancellationToken = default
        ) {
            var request = new CreateAnnotationRequest() { 
                Tag = tagId,
                Annotation = annotation
            };

            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.CreateAnnotation(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }


        private static async Task<IResult> UpdateAnnotationAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string tagId,
            string annotationId,
            TagValueAnnotation annotation,
            CancellationToken cancellationToken = default
        ) {
            var request = new UpdateAnnotationRequest() {
                Tag = tagId,
                AnnotationId = annotationId,
                Annotation = annotation
            };

            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.UpdateAnnotation(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }


        private static async Task<IResult> DeleteAnnotationAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            string tagId,
            string annotationId,
            CancellationToken cancellationToken = default
        ) {
            var request = new DeleteAnnotationRequest() {
                Tag = tagId,
                AnnotationId = annotationId
            };

            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.DeleteAnnotation(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }

    }
}
