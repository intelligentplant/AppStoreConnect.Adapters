using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;
using DataCore.Adapter.RealTimeData;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AdapterAccessorTests : TestsBase {

        [TestMethod]
        public async Task AdapterAccessor_ShouldReturnEnabledAdapter() {
            using (var adapter = new ExampleAdapter()) {
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var a = await accessor.GetAdapter(context, adapter.Descriptor.Id);
                Assert.IsNotNull(a);
            }
        }


        [TestMethod]
        public async Task AdapterAccessor_ShouldNotReturnDisabledAdapter() {
            using (var adapter = new ExampleAdapter()) {
                adapter.IsEnabled = false;
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var a = await accessor.GetAdapter(context, adapter.Descriptor.Id);
                Assert.IsNull(a);
            }
        }


        [TestMethod]
        public async Task AdapterAccessor_ShouldReturnDisabledAdapter() {
            using (var adapter = new ExampleAdapter()) {
                adapter.IsEnabled = false;
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var a = await accessor.GetAdapter(context, adapter.Descriptor.Id, false);
                Assert.IsNotNull(a);
            }
        }


        [TestMethod]
        public async Task AdapterAccessor_ShouldResolveAndAuthorizeFeatureType() {
            using (var adapter = new ExampleAdapter()) {
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var resolved = await accessor.GetAdapterAndFeature<IReadSnapshotTagValues>(context, adapter.Descriptor.Id);

                Assert.IsTrue(resolved.IsAdapterResolved);
                Assert.IsTrue(resolved.IsFeatureResolved);
                Assert.IsTrue(resolved.IsFeatureAuthorized);
            }
        }


        [TestMethod]
        public async Task AdapterAccessor_ShouldResolveButNotAuthorizeFeatureType() {
            using (var adapter = new ExampleAdapter()) {
                var authService = new AuthorizationService(true, false);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var resolved = await accessor.GetAdapterAndFeature<IReadSnapshotTagValues>(context, adapter.Descriptor.Id);

                Assert.IsTrue(resolved.IsAdapterResolved);
                Assert.IsTrue(resolved.IsFeatureResolved);
                Assert.IsFalse(resolved.IsFeatureAuthorized);
            }
        }


        [TestMethod]
        public async Task AdapterAccessor_ShouldNotResolveMissingFeatureType() {
            using (var adapter = new ExampleAdapter()) {
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var resolved = await accessor.GetAdapterAndFeature<IReadRawTagValues>(context, adapter.Descriptor.Id);

                Assert.IsTrue(resolved.IsAdapterResolved);
                Assert.IsFalse(resolved.IsFeatureResolved);
            }
        }


        [TestMethod]
        public async Task AdapterAccessor_ShouldResolveAndAuthorizeStandardFeatureUri() {
            using (var adapter = new ExampleAdapter()) {
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var resolved = await accessor.GetAdapterAndFeature(context, adapter.Descriptor.Id, WellKnownFeatures.RealTimeData.ReadSnapshotTagValues);

                Assert.IsTrue(resolved.IsAdapterResolved);
                Assert.IsTrue(resolved.IsFeatureResolved);
                Assert.IsTrue(resolved.IsFeatureAuthorized);
                Assert.IsTrue(resolved.Feature is IReadSnapshotTagValues);
            }
        }


        [TestMethod]
        public async Task AdapterAccessor_ShouldResolveAndAuthorizeExtensionFeatureUri() {
            using (var adapter = new ExampleAdapter()) {
                ((AdapterFeaturesCollection) adapter.Features).AddFromProvider(new TestExtension());
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var resolved = await accessor.GetAdapterAndFeature(
                    context, 
                    adapter.Descriptor.Id, 
                    WellKnownFeatures.Extensions.ExtensionFeatureBasePath + ExtensionFeatureUri
                );

                Assert.IsTrue(resolved.IsAdapterResolved);
                Assert.IsTrue(resolved.IsFeatureResolved);
                Assert.IsTrue(resolved.IsFeatureAuthorized);
                Assert.IsTrue(resolved.Feature is TestExtension);
            }
        }


        [TestMethod]
        public async Task AdapterAccessor_ShouldNotResolveMissingFeatureName() {
            using (var adapter = new ExampleAdapter()) {
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var resolved = await accessor.GetAdapterAndFeature(context, adapter.Descriptor.Id, WellKnownFeatures.RealTimeData.ReadRawTagValues);

                Assert.IsTrue(resolved.IsAdapterResolved);
                Assert.IsFalse(resolved.IsFeatureResolved);
            }
        }

        private const string ExtensionFeatureUri = "unit-test/test-extension";


        [ExtensionFeature(ExtensionFeatureUri)]
        private class TestExtension : AdapterExtensionFeature {

            public TestExtension() : base(null) {
                BindInvoke(GetCurrentTime);
            }


            public DateTime GetCurrentTime(IAdapterCallContext context) {
                return DateTime.UtcNow;
            }

        }


        private class AdapterAccessorImpl : AdapterAccessor {

            private readonly IAdapter _adapter;


            public AdapterAccessorImpl(IAdapter adapter, IAdapterAuthorizationService authService) : base(adapter.BackgroundTaskService, authService) {
                _adapter = adapter;
            }


            protected override Task<ChannelReader<IAdapter>> GetAdapters(CancellationToken cancellationToken) {
                var channel = new[] { _adapter }.PublishToChannel();
                return Task.FromResult(channel);
            }
        }


        private class AuthorizationService : IAdapterAuthorizationService {

            private readonly bool _authorizeAdapter;

            private readonly bool _authorizeFeature;


            public AuthorizationService(bool authorize) : this(authorize, authorize) { }

            public AuthorizationService(bool authorizeAdapter, bool authorizeFeature) {
                _authorizeAdapter = authorizeAdapter;
                _authorizeFeature = authorizeFeature;
            }

            public Task<bool> AuthorizeAdapter(IAdapter adapter, IAdapterCallContext context, CancellationToken cancellationToken) {
                return Task.FromResult(_authorizeAdapter);
            }

            public Task<bool> AuthorizeAdapterFeature(IAdapter adapter, IAdapterCallContext context, Uri featureUri, CancellationToken cancellationToken) {
                return Task.FromResult(_authorizeFeature);
            }
        }

    }
}
