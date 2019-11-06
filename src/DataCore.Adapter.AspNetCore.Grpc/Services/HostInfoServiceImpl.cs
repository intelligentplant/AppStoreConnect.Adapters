using System.Threading.Tasks;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class HostInfoServiceImpl : HostInfoService.HostInfoServiceBase {

        private readonly HostInfo _hostInfo;


        public HostInfoServiceImpl(Adapter.Common.HostInfo hostInfo) {
            _hostInfo = hostInfo.ToGrpcHostInfo();
        }


        public override Task<GetHostInfoResponse> GetHostInfo(GetHostInfoRequest request, ServerCallContext context) {
            var result = new GetHostInfoResponse() {
                HostInfo = _hostInfo
            };
            return Task.FromResult(result);
        }

    }
}
