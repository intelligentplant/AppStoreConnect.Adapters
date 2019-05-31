using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var feature = await Util.GetAdapterFeature<IReadTagValueAnnotations>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var reader = feature.ReadTagValueAnnotations(_adapterCallContext, new RealTimeData.Models.ReadAnnotationsRequest() {
                UtcStartTime = request.UtcStartTime.ToDateTime(),
                UtcEndTime = request.UtcEndTime.ToDateTime(),
                Tags = request.Tags?.ToArray() ?? new string[0]
            }, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var val) || val == null) {
                    continue;
                }

                await responseStream.WriteAsync(val.ToGrpcTagValueAnnotation()).ConfigureAwait(false);
            }

        }

    }
}
