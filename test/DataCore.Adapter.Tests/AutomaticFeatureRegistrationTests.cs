using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.RealTimeData;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    [TestClass]
    public class AutomaticFeatureRegistrationTests : TestsBase {

        [TestMethod]
        public void AdapterFeaturesShouldBeRegisteredByDefaultWhenInheritingFromAdapterBaseTOptions() {
            // Ensures that automatic feature registration is enabled when inheriting from
            // AdapterBase<TOptions>.
            using (var adapter = new FeatureRegistrationEnabledAdapter(TestContext.TestName)) {
                Assert.IsTrue(adapter.TryGetFeature<IReadSnapshotTagValues>(out var _));
            }
        }


        [TestMethod]
        public void AdapterFeaturesShouldNotBeRegisteredWhenAutomaticRegistrationIsExplicitlyDisabled() {
            // Ensures that automatic feature registration is disabled when the adapter class is
            // annotated with [AutomaticFeatureRegistration(false)].
            using (var adapter = new FeatureRegistrationDisabledAdapter(TestContext.TestName)) {
                Assert.IsFalse(adapter.TryGetFeature<IReadSnapshotTagValues>(out var _));
            }
        }


        [TestMethod]
        public void AdapterFeaturesShouldBeRegisteredWhenAutomaticRegistrationIsExplicitlyEnabled() {
            // Ensures that automatic feature registration is enabled when the adapter class is
            // annotated with [AutomaticFeatureRegistration(true)], even if the base class is
            // annotated with [AutomaticFeatureRegistration(false)].
            using (var adapter = new FeatureRegistrationEnabledAdapter2(TestContext.TestName)) {
                Assert.IsTrue(adapter.TryGetFeature<IReadSnapshotTagValues>(out var _));
            }
        }


        [TestMethod]
        public void AdapterFeaturesShouldNotBeRegisteredWhenAutomaticRegistrationIsImplicitlyDisabled() {
            // Ensures that automatic feature registration is disabled by default when inheriting
            // from a base class that is annotated with [AutomaticFeatureRegistration(false)].
            using (var adapter = new FeatureRegistrationDisabledAdapter2(TestContext.TestName)) {
                Assert.IsFalse(adapter.TryGetFeature<IReadSnapshotTagValues>(out var _));
            }
        }


        private abstract class TestAdapterBase : AdapterBase, IReadSnapshotTagValues {

            protected TestAdapterBase(string id) : base(id, null, null, null, null) { }

            protected override Task StartAsync(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }

            protected override Task StopAsync(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }

            public IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(IAdapterCallContext context, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken) {
                return Array.Empty<TagValueQueryResult>().PublishToChannel().ReadAllAsync(cancellationToken);
            }

        }


        private class FeatureRegistrationEnabledAdapter : TestAdapterBase {

            public FeatureRegistrationEnabledAdapter(string id) : base(id) { }

        }


        [AutomaticFeatureRegistration(false)]
        private class FeatureRegistrationDisabledAdapter : TestAdapterBase {

            public FeatureRegistrationDisabledAdapter(string id) : base(id) { }

        }


        [AutomaticFeatureRegistration]
        private class FeatureRegistrationEnabledAdapter2 : FeatureRegistrationDisabledAdapter {

            public FeatureRegistrationEnabledAdapter2(string id) : base(id) { }

        }


        private class FeatureRegistrationDisabledAdapter2 : FeatureRegistrationDisabledAdapter {

            public FeatureRegistrationDisabledAdapter2(string id) : base(id) { }

        }

    }
}
