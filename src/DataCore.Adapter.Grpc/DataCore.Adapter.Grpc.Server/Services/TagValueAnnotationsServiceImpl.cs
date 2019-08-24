using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class TagValueAnnotationsServiceImpl : TagValueAnnotationsService.TagValueAnnotationsServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public TagValueAnnotationsServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        public override async Task ReadAnnotations(ReadAnnotationsRequest request, IServerStreamWriter<TagValueAnnotationQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.ReadAnnotationsRequest() {
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                Tags = request.Tags?.ToArray() ?? new string[0]
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.ReadAnnotations(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueAnnotationQueryResult()).ConfigureAwait(false);
            }
        }


        public override async Task<TagValueAnnotation> ReadAnnotation(ReadAnnotationRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.ReadAnnotationRequest() {
                TagId = request.TagId,
                AnnotationId = request.AnnotationId
            };
            Util.ValidateObject(adapterRequest);

            var result = await adapter.Feature.ReadAnnotation(_adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
            return result.ToGrpcTagValueAnnotation();
        }


        public override async Task<WriteTagValueAnnotationResult> CreateAnnotation(CreateAnnotationRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.CreateAnnotationRequest() {
                TagId = request.TagId,
                Annotation = request.Annotation.ToAdapterTagValueAnnotation()
            };
            Util.ValidateObject(adapterRequest);

            var result = await adapter.Feature.CreateAnnotation(_adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
            return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
        }


        public override async Task<WriteTagValueAnnotationResult> UpdateAnnotation(UpdateAnnotationRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.UpdateAnnotationRequest() {
                TagId = request.TagId,
                AnnotationId = request.AnnotationId,
                Annotation = request.Annotation.ToAdapterTagValueAnnotation()
            };
            Util.ValidateObject(adapterRequest);

            var result = await adapter.Feature.UpdateAnnotation(_adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
            return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
        }


        public override async Task<WriteTagValueAnnotationResult> DeleteAnnotation(DeleteAnnotationRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.Models.DeleteAnnotationRequest() {
                TagId = request.TagId,
                AnnotationId = request.AnnotationId
            };
            Util.ValidateObject(adapterRequest);

            var result = await adapter.Feature.DeleteAnnotation(_adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
            return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
        }

    }
}
