using DataCore.Adapter.Grpc.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class GrpcProxyTests : ProxyAdapterTests<GrpcAdapterProxy> {

        protected override GrpcAdapterProxy CreateProxy(string remoteAdapterId) {
            return ActivatorUtilities.CreateInstance<GrpcAdapterProxy>(ServiceProvider, nameof(GrpcProxyTests), new GrpcAdapterProxyOptions() {
                RemoteId = remoteAdapterId
            });
        }

    }
}
