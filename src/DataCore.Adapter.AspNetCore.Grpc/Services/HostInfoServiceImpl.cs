using System.Threading.Tasks;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {
    public class HostInfoServiceImpl : HostInfoService.HostInfoServiceBase {

        private readonly HostInfo _hostInfo;


        public HostInfoServiceImpl(Adapter.Common.HostInfo hostInfo) {
            _hostInfo = hostInfo == null
                ? null
                : new HostInfo() {
                    Description = hostInfo.Description,
                    Name = hostInfo.Name,
                    VendorInfo = new VendorInfo() {
                        Name = hostInfo.Vendor?.Name,
                        Url = hostInfo.Vendor?.Url.ToString()
                    },
                    Version = hostInfo.Version
                };

            if (hostInfo.Properties != null) {
                foreach (var item in hostInfo.Properties) {
                    if (item == null) {
                        continue;
                    }
                    _hostInfo.Properties.Add(item.ToGrpcProperty());
                }
            }
        }


        public override Task<GetHostInfoResponse> GetHostInfo(GetHostInfoRequest request, ServerCallContext context) {
            var result = new GetHostInfoResponse() {
                HostInfo = _hostInfo
            };
            return Task.FromResult(result);
        }

    }
}
