#if NET7_0_OR_GREATER

using System;

using DataCore.Adapter.Http.Proxy;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class HttpProxyMinimalApiTests : HttpProxyTests {

        protected override HttpAdapterProxy CreateProxy(TestContext context, string remoteAdapterId, IServiceProvider serviceProvider) {
            var result = base.CreateProxy(context, remoteAdapterId, serviceProvider);

            result.GetClient().BasePath = "/minimal-api/unit-tests/v2.0";

            return result;
        }

    }

}

#endif
