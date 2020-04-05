using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="AdaptersService.AdaptersServiceBase"/>.
    /// </summary>
    public class AdaptersServiceImpl : AdaptersService.AdaptersServiceBase {

        /// <summary>
        /// The <see cref="IAdapterCallContext"/> for the caller.
        /// </summary>
        private readonly IAdapterCallContext _adapterCallContext;

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;


        /// <summary>
        /// Creates a new <see cref="AdaptersServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterCallContext">
        ///   The <see cref="IAdapterCallContext"/> for the caller.
        /// </param>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public AdaptersServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task<FindAdaptersResponse> FindAdapters(FindAdaptersRequest request, ServerCallContext context) {
            var adapters = await _adapterAccessor.FindAdapters(
                _adapterCallContext, 
                new Common.FindAdaptersRequest() {
                    Id = request.Id,
                    Name = request.Name,
                    Description = request.Description,
                    PageSize = request.PageSize,
                    Page = request.Page
                },
                context.CancellationToken
            ).ConfigureAwait(false);

            var result = new FindAdaptersResponse();
            result.Adapters.AddRange(adapters.Select(x => x.Descriptor.ToGrpcAdapterDescriptor()));

            return result;
        }


        /// <inheritdoc/>
        public override async Task<GetAdapterResponse> GetAdapter(GetAdapterRequest request, ServerCallContext context) {
            var adapter = await _adapterAccessor.GetAdapter(_adapterCallContext, request.AdapterId, context.CancellationToken).ConfigureAwait(false);

            return new GetAdapterResponse() {
                Adapter = adapter?.CreateExtendedAdapterDescriptor().ToGrpcExtendedAdapterDescriptor()
            };
        }


        /// <inheritdoc/>
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
