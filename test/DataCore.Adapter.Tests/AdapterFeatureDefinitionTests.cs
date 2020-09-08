using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {
    [TestClass]
    public class AdapterFeatureDefinitionTests : TestsBase {

        [TestMethod]
        public void StandardAdapterFeatureTypesShouldHaveAdapterFeatureAttribute() {
            foreach (var featureType in TypeExtensions.GetStandardAdapterFeatureTypes()) {
                Assert.IsNotNull(featureType.GetAdapterFeatureUri(), $"Feature type {featureType.Name} is not annotated with {nameof(AdapterFeatureAttribute)}.");
            }
        }


        [TestMethod]
        public void ShouldResolveStandardFeatureDescriptorFromUri() {
            foreach (var featureType in TypeExtensions.GetStandardAdapterFeatureTypes()) {
                var uri = featureType.GetAdapterFeatureUri();
                Assert.IsTrue(WellKnownFeatures.TryGetFeatureDescriptor(uri, out var descriptor), $"Should have resolved descriptor for {uri}");
                Assert.IsNotNull(descriptor, $"Decriptor for {uri} should not be null.");
                Assert.AreEqual(uri, descriptor.Uri);
            }
        }

    }
}
