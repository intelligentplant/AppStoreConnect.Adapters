using DataCore.Adapter.AspNetCore.Internal;
using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class TagAnnotationRoutes : IRouteProvider {

        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}", ReadAnnotationsAsync)
                .Produces<IAsyncEnumerable<TagValueAnnotationQueryResult>>()
                .ProducesDefaultErrors();

            builder.MapGet($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/{{tagId}}/{{annotationId}}", ReadAnnotationAsync)
                .Produces<TagValueAnnotationExtended>()
                .ProducesDefaultErrors();

            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/get-by-id", ReadAnnotationRequestAsync)
                .Produces<TagValueAnnotationExtended>()
                .ProducesDefaultErrors();

            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/{{tagId}}/create", CreateAnnotationAsync)
                .Produces<WriteTagValueAnnotationResult>()
                .ProducesDefaultErrors();

            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/create", CreateAnnotationRequestAsync)
                .Produces<WriteTagValueAnnotationResult>()
                .ProducesDefaultErrors();

            builder.MapPut($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/{{tagId}}/{{annotationId}}", UpdateAnnotationAsync)
                .Produces<WriteTagValueAnnotationResult>()
                .ProducesDefaultErrors();

            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/update", UpdateAnnotationRequestAsync)
                .Produces<WriteTagValueAnnotationResult>()
                .ProducesDefaultErrors();

            builder.MapDelete($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/{{tagId}}/{{annotationId}}", DeleteAnnotationAsync)
                .Produces<WriteTagValueAnnotationResult>()
                .ProducesDefaultErrors();

            builder.MapPost($"/{{adapterId:maxlength({AdapterDescriptor.IdMaxLength})}}/delete", DeleteAnnotationRequestAsync)
                .Produces<WriteTagValueAnnotationResult>()
                .ProducesDefaultErrors();
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


        private static Task<IResult> ReadAnnotationAsync(
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

            return ReadAnnotationRequestAsync(context, adapterAccessor, adapterId, request, cancellationToken);
        }


        private static async Task<IResult> ReadAnnotationRequestAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            ReadAnnotationRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IReadTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }
            return Results.Ok(await resolverResult.Feature.ReadAnnotation(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }


        private static Task<IResult> CreateAnnotationAsync(
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

            return CreateAnnotationRequestAsync(context, adapterAccessor, adapterId, request, cancellationToken);
        }


        private static async Task<IResult> CreateAnnotationRequestAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            CreateAnnotationRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.CreateAnnotation(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }


        private static Task<IResult> UpdateAnnotationAsync(
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

            return UpdateAnnotationRequestAsync(context, adapterAccessor, adapterId, request, cancellationToken);
        }


        private static async Task<IResult> UpdateAnnotationRequestAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            UpdateAnnotationRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.UpdateAnnotation(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }


        private static Task<IResult> DeleteAnnotationAsync(
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

            return DeleteAnnotationRequestAsync(context, adapterAccessor, adapterId, request, cancellationToken);
        }


        private static async Task<IResult> DeleteAnnotationRequestAsync(
            HttpContext context,
            IAdapterAccessor adapterAccessor,
            string adapterId,
            DeleteAnnotationRequest request,
            CancellationToken cancellationToken = default
        ) {
            var resolverResult = await Utils.ResolveAdapterAndValidateRequestAsync<IWriteTagValueAnnotations>(context, adapterAccessor, adapterId, request, true, cancellationToken).ConfigureAwait(false);
            if (resolverResult.Error != null) {
                return resolverResult.Error;
            }

            return Results.Ok(await resolverResult.Feature.DeleteAnnotation(resolverResult.CallContext, request, cancellationToken).ConfigureAwait(false));
        }

    }
}
