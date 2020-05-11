using System.Linq;
using System.Threading.Tasks;
using DataCore.Adapter.AspNetCore.Grpc;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="AdaptersService.AdaptersServiceBase"/>.
    /// </summary>
    public class AdaptersServiceImpl : AdaptersService.AdaptersServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="AdaptersServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public AdaptersServiceImpl(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task<FindAdaptersResponse> FindAdapters(FindAdaptersRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapters = await _adapterAccessor.FindAdapters(
                adapterCallContext, 
                new Common.FindAdaptersRequest() {
                    Id = request.Id,
                    Name = request.Name,
                    Description = request.Description,
                    Features = request.Features.ToArray(),
                    PageSize = request.PageSize,
                    Page = request.Page
                },
                true,
                context.CancellationToken
            ).ConfigureAwait(false);

            var result = new FindAdaptersResponse();
            result.Adapters.AddRange(adapters.Select(x => x.Descriptor.ToGrpcAdapterDescriptor()));

            return result;
        }


        /// <inheritdoc/>
        public override async Task<GetAdapterResponse> GetAdapter(GetAdapterRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapter = await _adapterAccessor.GetAdapter(adapterCallContext, request.AdapterId, true, context.CancellationToken).ConfigureAwait(false);

            return new GetAdapterResponse() {
                Adapter = adapter?.CreateExtendedAdapterDescriptor().ToGrpcExtendedAdapterDescriptor()
            };
        }


        /// <inheritdoc/>
        public override async Task<CheckAdapterHealthResponse> CheckAdapterHealth(CheckAdapterHealthRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<Diagnostics.IHealthCheck>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);
            
            var result = await adapter.Feature.CheckHealthAsync(adapterCallContext, context.CancellationToken).ConfigureAwait(false);

            return new CheckAdapterHealthResponse() { 
                Result = result.ToGrpcHealthCheckResult()
            };
        }

    }
}
