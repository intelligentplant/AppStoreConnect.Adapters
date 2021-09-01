using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.RealTimeData;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AdapterAccessorTests : TestsBase {

        [TestMethod]
        public async Task AdapterAccessor_ShouldReturnAdapter() {
            using (var adapter = new ExampleAdapter()) {
                var authService = new AuthorizationService(true);
                IAdapterAccessor accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var a = await accessor.GetAdapter(context, adapter.Descriptor.Id, default);
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
                adapter.AddFeatures(new TestExtension());
                var authService = new AuthorizationService(true);
                var accessor = new AdapterAccessorImpl(adapter, authService);

                var context = ExampleCallContext.ForPrincipal(null);
                var resolved = await accessor.GetAdapterAndFeature(
                    context, 
                    adapter.Descriptor.Id, 
                    WellKnownFeatures.Extensions.BaseUri + ExtensionFeatureUri
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
                BindInvoke<TestExtension>((ctx, req, ct) => {
                    return Task.FromResult(new InvocationResponse() { 
                        Results = SerializeToJsonElement(GetCurrentTime())
                    });
                }, nameof(GetCurrentTime));
            }


            public DateTime GetCurrentTime() {
                return DateTime.UtcNow;
            }

        }


        private class AdapterAccessorImpl : AdapterAccessor {

            private readonly IAdapter _adapter;


            public AdapterAccessorImpl(IAdapter adapter, IAdapterAuthorizationService authService) : base(authService) {
                _adapter = adapter;
            }


            protected override async IAsyncEnumerable<IAdapter> FindAdapters(
                IAdapterCallContext context, 
                FindAdaptersRequest request, 
                [EnumeratorCancellation]
                CancellationToken cancellationToken
            ) {
                if (request.Page > 1 || !MatchesFilter(_adapter, request) || !await IsAuthorized(_adapter, context, cancellationToken).ConfigureAwait(false)) {
                    yield break;
                }

                yield return _adapter;
            }


            protected override Task<IAdapter> GetAdapter(IAdapterCallContext context, string adapterId, CancellationToken cancellationToken) {
                if (string.Equals(adapterId, _adapter.Descriptor.Id, StringComparison.OrdinalIgnoreCase)) {
                    return Task.FromResult(_adapter);
                }

                return Task.FromResult<IAdapter>(null);
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
