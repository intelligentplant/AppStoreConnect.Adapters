using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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


        private class AdapterAccessorImpl : AdapterAccessor {

            private readonly IAdapter _adapter;


            public AdapterAccessorImpl(IAdapter adapter, IAdapterAuthorizationService authService) : base(authService) {
                _adapter = adapter;
            }


            protected override Task<IEnumerable<IAdapter>> GetAdapters(CancellationToken cancellationToken) {
                return Task.FromResult<IEnumerable<IAdapter>>(new[] { _adapter });
            }
        }


        private class AuthorizationService : IAdapterAuthorizationService {

            private readonly bool _authorize;

            public AuthorizationService(bool authorize) {
                _authorize = authorize;
            }

            public Task<bool> AuthorizeAdapter(IAdapter adapter, IAdapterCallContext context, CancellationToken cancellationToken) {
                return Task.FromResult(_authorize);
            }

            public Task<bool> AuthorizeAdapterFeature<TFeature>(IAdapter adapter, IAdapterCallContext context, CancellationToken cancellationToken) where TFeature : IAdapterFeature {
                return Task.FromResult(_authorize);
            }
        }

    }
}
