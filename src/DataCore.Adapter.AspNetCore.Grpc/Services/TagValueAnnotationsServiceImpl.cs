using System;
using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="TagValueAnnotationsService.TagValueAnnotationsServiceBase"/>.
    /// </summary>
    public class TagValueAnnotationsServiceImpl : TagValueAnnotationsService.TagValueAnnotationsServiceBase {

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the caller.
        /// </summary>
        private readonly IAdapterCallContext _adapterCallContext;

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="TagValueAnnotationsServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterCallContext">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public TagValueAnnotationsServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task ReadAnnotations(ReadAnnotationsRequest request, IServerStreamWriter<TagValueAnnotationQueryResult> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadAnnotationsRequest() {
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                Tags = request.Tags?.ToArray() ?? Array.Empty<string>()
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


        /// <inheritdoc/>
        public override async Task<TagValueAnnotation> ReadAnnotation(ReadAnnotationRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IReadTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.ReadAnnotationRequest() {
                Tag = request.Tag,
                AnnotationId = request.AnnotationId
            };
            Util.ValidateObject(adapterRequest);

            var result = await adapter.Feature.ReadAnnotation(_adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
            return result.ToGrpcTagValueAnnotation();
        }


        /// <inheritdoc/>
        public override async Task<WriteTagValueAnnotationResult> CreateAnnotation(CreateAnnotationRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.CreateAnnotationRequest() {
                Tag = request.Tag,
                Annotation = request.Annotation.ToAdapterTagValueAnnotation()
            };
            Util.ValidateObject(adapterRequest);

            var result = await adapter.Feature.CreateAnnotation(_adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
            return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
        }


        /// <inheritdoc/>
        public override async Task<WriteTagValueAnnotationResult> UpdateAnnotation(UpdateAnnotationRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.UpdateAnnotationRequest() {
                Tag = request.Tag,
                AnnotationId = request.AnnotationId,
                Annotation = request.Annotation.ToAdapterTagValueAnnotation()
            };
            Util.ValidateObject(adapterRequest);

            var result = await adapter.Feature.UpdateAnnotation(_adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
            return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
        }


        /// <inheritdoc/>
        public override async Task<WriteTagValueAnnotationResult> DeleteAnnotation(DeleteAnnotationRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IWriteTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new RealTimeData.DeleteAnnotationRequest() {
                Tag = request.Tag,
                AnnotationId = request.AnnotationId
            };
            Util.ValidateObject(adapterRequest);

            var result = await adapter.Feature.DeleteAnnotation(_adapterCallContext, adapterRequest, cancellationToken).ConfigureAwait(false);
            return result.ToGrpcWriteTagValueAnnotationResult(adapter.Adapter.Descriptor.Id);
        }

    }
}
