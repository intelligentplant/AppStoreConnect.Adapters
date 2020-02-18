using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class TagSearchServiceImpl : TagSearchService.TagSearchServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public TagSearchServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        public override async Task GetTagProperties(GetTagPropertiesRequest request, IServerStreamWriter<AdapterProperty> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagInfo>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.RealTimeData.GetTagPropertiesRequest() {
                PageSize = request.PageSize,
                Page = request.Page
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.GetTagProperties(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var prop) || prop == null) {
                    continue;
                }

                await responseStream.WriteAsync(prop.ToGrpcAdapterProperty()).ConfigureAwait(false);
            }
        }


        public override async Task FindTags(FindTagsRequest request, IServerStreamWriter<TagDefinition> responseStream, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ITagSearch>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.RealTimeData.FindTagsRequest() {
                Name = request.Name,
                Description = request.Description,
                Units = request.Units,
                Label = request.Label,
                Other = request.Other,
                PageSize = request.PageSize,
                Page = request.Page
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.FindTags(_adapterCallContext, adapterRequest, cancellationToken);

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
            var adapter = await Util.ResolveAdapterAndFeature<ITagInfo>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var adapterRequest = new Adapter.RealTimeData.GetTagsRequest() {
                Tags = request.Tags.ToArray()
            };
            Util.ValidateObject(adapterRequest);

            var reader = adapter.Feature.GetTags(_adapterCallContext, adapterRequest, cancellationToken);

            while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                if (!reader.TryRead(out var tag) || tag == null) {
                    continue;
                }

                await responseStream.WriteAsync(tag.ToGrpcTagDefinition()).ConfigureAwait(false);
            }
        }

    }
}
