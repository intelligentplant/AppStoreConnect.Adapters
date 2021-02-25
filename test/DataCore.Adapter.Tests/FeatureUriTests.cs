using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class FeatureUriTests : TestsBase {
    
        [TestMethod]
        public void FeatureUriWithHashFragmentShouldBeRecognisedAsStandardFeature() {
            var uri = new Uri(string.Concat(WellKnownFeatures.RealTimeData.ReadProcessedTagValues, "#", RealTimeData.DefaultDataFunctions.Average.Id));
            Assert.IsTrue(uri.IsStandardFeatureUri());
        }
    
    }

}
