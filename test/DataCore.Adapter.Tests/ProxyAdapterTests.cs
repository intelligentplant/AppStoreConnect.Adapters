using System;
using System.Threading.Tasks;
using DataCore.Adapter.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    public abstract class ProxyAdapterTests<TProxy> : AdapterTests<TProxy> where TProxy : class, IAdapterProxy {

        protected sealed override TProxy CreateAdapter() {
            return CreateProxy(WebHostStartup.AdapterId);
        }


        protected sealed override ReadTagValuesQueryDetails GetReadTagValuesQueryDetails() {
            var now = DateTime.UtcNow;
            return new ReadTagValuesQueryDetails(WebHostStartup.TestTagId) { 
                HistoryStartTime = now.AddDays(-1),
                HistoryEndTime = now
            };
        }


        protected sealed override ReadEventMessagesQueryDetails GetReadEventMessagesQueryDetails() {
            // TODO: return correct details.
            return null;
        }


        protected override Task EmitTestEvent(TProxy adapter, EventMessageSubscriptionType subscriptionType) {
            // TODO: tell the "remote" adapter to emit a test event.
            return Task.CompletedTask;
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
