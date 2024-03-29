﻿#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.Http.Proxy;
using DataCore.Adapter.RealTimeData;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    public abstract class HttpProxyTests : ProxyAdapterTests<HttpAdapterProxy> {

        protected override IEnumerable<string> UnsupportedStandardFeatures {
            get {
                // HTTP proxy does not currently support any push-based features.
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


        protected override HttpAdapterProxy CreateProxy(TestContext context, string remoteAdapterId, IServiceProvider serviceProvider) {
            var options = new HttpAdapterProxyOptions() {
                RemoteId = remoteAdapterId,
                SignalROptions = new SignalROptions() {
                    ConnectionFactory = (url, ctx) => new HubConnectionBuilder()
                        .WithDataCoreAdapterConnection(url, options => {
                            options.HttpMessageHandlerFactory = handler => {
                                WebHostConfiguration.AllowUntrustedCertificates(handler);
                                return handler;
                            };
                        })
                        .WithAutomaticReconnect()
                        .Build()
                }
            };

            if (string.Equals(context.TestName, nameof(HttpProxyShouldNotEnableSnapshotPushWhenRepollingIntervalIsZero))) {
                options.SignalROptions = null;
                options.TagValuePushInterval = TimeSpan.Zero;
            }
            else if (string.Equals(context.TestName, nameof(HttpProxyShouldNotEnableSnapshotPushWhenRepollingIntervalIsNegative))) {
                options.SignalROptions = null;
                options.TagValuePushInterval = TimeSpan.FromSeconds(-1);
            }

            return ActivatorUtilities.CreateInstance<HttpAdapterProxy>(serviceProvider, nameof(HttpProxyTests), options);
        }


        [TestMethod]
        public Task HttpProxyShouldNotEnableSnapshotPushWhenRepollingIntervalIsZero() {
            return RunAdapterTest((adapter, ctx, ct) => {
                Assert.IsFalse(adapter.HasFeature<ISnapshotTagValuePush>());
                return Task.CompletedTask;
            });
        }


        [TestMethod]
        public Task HttpProxyShouldNotEnableSnapshotPushWhenRepollingIntervalIsNegative() {
            return RunAdapterTest((adapter, ctx, ct) => {
                Assert.IsFalse(adapter.HasFeature<ISnapshotTagValuePush>());
                return Task.CompletedTask;
            });
        }

    }

}
#endif
