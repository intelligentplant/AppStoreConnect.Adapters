using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.Http.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class HttpProxyTests : ProxyAdapterTests<HttpAdapterProxy> {

        protected override IEnumerable<string> UnsupportedStandardFeatures {
            get {
                // HTTP proxy does not support any push-based features.
                yield return WellKnownFeatures.Diagnostics.ConfigurationChanges;
                yield return WellKnownFeatures.Events.EventMessagePush;
                yield return WellKnownFeatures.Events.EventMessagePushWithTopics;
                yield return WellKnownFeatures.RealTimeData.SnapshotTagValuePush;
            }
        }


        protected override IEnumerable<ExtensionFeatureOperationType> ExpectedExtensionFeatureOperationTypes() {
            return new[] {
                ExtensionFeatureOperationType.Invoke
            };
        }


        protected override HttpAdapterProxy CreateProxy(string remoteAdapterId, IServiceProvider serviceProvider) {
            return ActivatorUtilities.CreateInstance<HttpAdapterProxy>(serviceProvider, nameof(HttpProxyTests), new HttpAdapterProxyOptions() {
                RemoteId = remoteAdapterId
            });
        }

    }

}
