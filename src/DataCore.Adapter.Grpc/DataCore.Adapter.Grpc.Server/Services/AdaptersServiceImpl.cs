using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.Grpc;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class AdaptersServiceImpl : AdaptersService.AdaptersServiceBase {

        private readonly IAdapterCallContext _adapterCallContext;

        private readonly IAdapterAccessor _adapterAccessor;


        public AdaptersServiceImpl(IAdapterCallContext adapterCallContext, IAdapterAccessor adapterAccessor) {
            _adapterCallContext = adapterCallContext;
            _adapterAccessor = adapterAccessor;
        }


        private ExtendedAdapterDescriptor ToGrpcAdapterDescriptor(IAdapter adapter) {
            var source = adapter.CreateExtendedAdapterDescriptor();

            var result = new ExtendedAdapterDescriptor() {
                AdapterDescriptor = new AdapterDescriptor() {
                    Id = source.Id,
                    Name = source.Name,
                    Description = source.Description
                }
            };

            result.Features.AddRange(source.Features);
            result.Extensions.AddRange(source.Extensions);
            result.Properties.Add(source.Properties);
            return result;
        }


        public override async Task<GetAdaptersResponse> GetAdapters(GetAdaptersRequest request, ServerCallContext context) {
            var adapters = await _adapterAccessor.GetAdapters(_adapterCallContext, context.CancellationToken).ConfigureAwait(false);

            var result = new GetAdaptersResponse();
            result.Adapters.AddRange(adapters.Select(x => ToGrpcAdapterDescriptor(x)));

            return result;
        }


        public override async Task<GetAdapterResponse> GetAdapter(GetAdapterRequest request, ServerCallContext context) {
            var adapter = await _adapterAccessor.GetAdapter(_adapterCallContext, request.AdapterId, context.CancellationToken).ConfigureAwait(false);

            return new GetAdapterResponse() {
                Adapter = adapter == null 
                    ? null 
                    : ToGrpcAdapterDescriptor(adapter)
            };
        }

    }
}
