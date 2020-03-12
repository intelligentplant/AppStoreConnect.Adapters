using DataCore.Adapter.Http.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class HttpProxyTests : ProxyAdapterTests<HttpAdapterProxy> {

        protected override HttpAdapterProxy CreateProxy(string remoteAdapterId) {
            return ActivatorUtilities.CreateInstance<HttpAdapterProxy>(ServiceProvider, nameof(HttpProxyTests), new HttpAdapterProxyOptions() {
                RemoteId = remoteAdapterId
            });
        }

    }

}
