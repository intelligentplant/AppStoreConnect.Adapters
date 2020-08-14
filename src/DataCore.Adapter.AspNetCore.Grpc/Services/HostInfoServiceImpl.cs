using System.Threading.Tasks;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="HostInfoService.HostInfoServiceBase"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Arguments are passed by gRPC framework")]
    public class HostInfoServiceImpl : HostInfoService.HostInfoServiceBase {

        /// <summary>
        /// The application host information.
        /// </summary>
        private readonly HostInfo _hostInfo;


        /// <summary>
        /// Creates a new <see cref="HostInfoServiceImpl"/> object.
        /// </summary>
        /// <param name="hostInfo">
        ///   The application host information.
        /// </param>
        public HostInfoServiceImpl(Common.HostInfo hostInfo) {
            _hostInfo = hostInfo.ToGrpcHostInfo();
        }


        /// <inheritdoc/>
        public override Task<GetHostInfoResponse> GetHostInfo(GetHostInfoRequest request, ServerCallContext context) {
            var result = new GetHostInfoResponse() {
                HostInfo = _hostInfo
            };
            return Task.FromResult(result);
        }

    }
}
