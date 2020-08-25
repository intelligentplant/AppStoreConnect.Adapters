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

    }
}
