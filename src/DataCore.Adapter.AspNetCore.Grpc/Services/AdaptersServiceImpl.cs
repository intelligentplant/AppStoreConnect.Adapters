using System;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Diagnostics;

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
        public override async Task FindAdapters(FindAdaptersRequest request, IServerStreamWriter<FindAdaptersResponse> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapters = _adapterAccessor.FindAdapters(
                adapterCallContext,
                new Common.FindAdaptersRequest() {
                    Id = request.Id,
                    Name = request.Name,
                    Description = request.Description,
                    Features = request.Features.ToArray(),
                    PageSize = request.PageSize,
                    Page = request.Page
                },
                context.CancellationToken
            );

            await foreach (var item in adapters.ConfigureAwait(false)) {
                await responseStream.WriteAsync(new FindAdaptersResponse() {
                    Adapter = item.Descriptor.ToGrpcAdapterDescriptor()
                }).ConfigureAwait(false);
            }
        }


        /// <inheritdoc/>
        public override async Task<GetAdapterResponse> GetAdapter(GetAdapterRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapter = await _adapterAccessor.GetAdapter(adapterCallContext, request.AdapterId, context.CancellationToken).ConfigureAwait(false);

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

            var result = await adapter.Feature.CheckHealthAsync(adapterCallContext, cancellationToken).ConfigureAwait(false);

            return new CheckAdapterHealthResponse() {
                Result = result.ToGrpcHealthCheckResult()
            };
        }


        /// <inheritdoc/>
        public override async Task CreateAdapterHealthPushChannel(CreateAdapterHealthPushChannelRequest request, IServerStreamWriter<HealthCheckResult> responseStream, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<IHealthCheck>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            await foreach (var item in adapter.Feature.Subscribe(adapterCallContext, cancellationToken).ConfigureAwait(false)) {
                await responseStream.WriteAsync(item.ToGrpcHealthCheckResult()).ConfigureAwait(false);
            }
        }

    }
}
