using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Features;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class TagSearchServiceImpl : TagSearchService.TagSearchServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public TagSearchServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        public override async Task FindTags(FindTagsRequest request, IServerStreamWriter<TagDefinition> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagSearch>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var reader = adapter.Feature.FindTags(_adapterCallContext, new Adapter.RealTimeData.Models.FindTagsRequest() {
                Name = request.Name,
                Description = request.Description,
                Units = request.Units,
                Other = request.Other,
                PageSize = (int) request.PageSize,
                Page = (int) request.Page
            }, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var tag) || tag == null) {
                    continue;
                }

                await responseStream.WriteAsync(tag.ToGrpcTagDefinition()).ConfigureAwait(false);
            }
        }


        public override async Task GetTags(GetTagsRequest request, IServerStreamWriter<TagDefinition> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagSearch>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var reader = adapter.Feature.GetTags(_adapterCallContext, new Adapter.RealTimeData.Models.GetTagsRequest() {
                Tags = request.Tags.ToArray()
            }, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var tag) || tag == null) {
                    continue;
                }

                await responseStream.WriteAsync(tag.ToGrpcTagDefinition()).ConfigureAwait(false);
            }
        }

    }
}
