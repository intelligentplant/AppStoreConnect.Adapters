using System;
using System.Collections.Generic;
using System.Text;

using DataCore.Adapter.Extensions;

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
                if (uri.ToString().StartsWith("unit-tests:")) {
                    // Feature was added by another unit test.
                    continue;
                }
                Assert.IsTrue(WellKnownFeatures.TryGetFeatureDescriptor(uri, out var descriptor), $"Should have resolved descriptor for {uri}");
                Assert.IsNotNull(descriptor, $"Decriptor for {uri} should not be null.");
                Assert.AreEqual(uri, descriptor.Uri);
            }
        }


        [TestMethod]
        public void ShouldResolveExtensionFeatureFromOperationUri() {
            var featureUri = new Uri(new Uri(WellKnownFeatures.Extensions.BaseUri), string.Concat("unit-tests/", GetType().Name, "/"));
            var operationUri = new Uri(featureUri, "invoke/" + TestContext.TestName);

            var success = AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationUri, out var featureUriActual, out var error);
            if (!success) {
                Assert.Fail(error);
            }
            Assert.AreEqual(featureUri, featureUriActual);
        }


        [DataTestMethod]
        [DataRow(WellKnownFeatures.Extensions.BaseUri + "unit-tests/")]
        [DataRow(WellKnownFeatures.Extensions.BaseUri + "unit-tests/invoke/")]
        [DataRow(WellKnownFeatures.Extensions.BaseUri + "unit-tests/GetDescriptor")]
        [DataRow(WellKnownFeatures.Extensions.BaseUri + "unit-tests/GetOperations")]
        public void ShouldNotResolveFeatureFromOperationUri(string uri) {
            var operationUri = new Uri(uri);
            Assert.IsFalse(AdapterExtensionFeature.TryGetFeatureUriFromOperationUri(operationUri, out var _, out var _));
        }

    }
}
