using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class AdaptersServiceImpl : AdaptersService.AdaptersServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public AdaptersServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        public override async Task<GetAdaptersResponse> GetAdapters(GetAdaptersRequest request, ServerCallContext context) {
            var adapters = await _adapterAccessor.GetAdapters(_adapterCallContext, context.CancellationToken).ConfigureAwait(false);

            var result = new GetAdaptersResponse();
            result.Adapters.AddRange(adapters.Select(x => x.Descriptor.ToGrpcAdapterDescriptor()));

            return result;
        }


        public override async Task<GetAdapterResponse> GetAdapter(GetAdapterRequest request, ServerCallContext context) {
            var adapter = await _adapterAccessor.GetAdapter(_adapterCallContext, request.AdapterId, context.CancellationToken).ConfigureAwait(false);

            return new GetAdapterResponse() {
                Adapter = adapter?.CreateExtendedAdapterDescriptor().ToGrpcExtendedAdapterDescriptor()
            };
        }


        public override async Task<CheckAdapterHealthResponse> CheckAdapterHealth(CheckAdapterHealthRequest request, ServerCallContext context) {
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<Diagnostics.IHealthCheck>(_adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);
            
            var result = await adapter.Feature.CheckHealthAsync(_adapterCallContext, context.CancellationToken).ConfigureAwait(false);

            return new CheckAdapterHealthResponse() { 
                Result = result.ToGrpcHealthCheckResult()
            };
        }

    }
}
