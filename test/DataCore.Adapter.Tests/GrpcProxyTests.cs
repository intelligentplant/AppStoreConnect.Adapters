using System;
#if NETCOREAPP
using DataCore.Adapter.Grpc.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class GrpcProxyTests : ProxyAdapterTests<GrpcAdapterProxy> {

        protected override GrpcAdapterProxy CreateProxy(string remoteAdapterId, IServiceProvider serviceProvider) {

            return ActivatorUtilities.CreateInstance<GrpcAdapterProxy>(serviceProvider, nameof(GrpcProxyTests), new GrpcAdapterProxyOptions() {
                RemoteId = remoteAdapterId
            });
        }

    }
}
#endif
