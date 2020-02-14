using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    public abstract class ProxyAdapterTests<TProxy> : AdapterTests<TProxy> where TProxy : class, IAdapterProxy {

        protected sealed override TProxy CreateAdapter() {
            return CreateProxy(WebHostStartup.AdapterId);
        }

        protected sealed override TestTagDetails GetTestTagDetails() {
            var now = DateTime.UtcNow;
            return new TestTagDetails(WebHostStartup.TestTagId) { 
                HistoryStartTime = now.AddDays(-1),
                HistoryEndTime = now
            };
        }


        protected abstract TProxy CreateProxy(string remoteAdapterId);


        [TestMethod]
        public Task ProxyShouldRetrieveRemoteAdapterDetails() {
            return RunAdapterTest((proxy, context) => {
                Assert.IsNotNull(proxy.RemoteHostInfo);
                Assert.IsNotNull(proxy.RemoteDescriptor);

                return Task.CompletedTask;
            });
        }

    }
}
