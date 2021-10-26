using System.Text.Json;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.Json;
using DataCore.Adapter.RealTimeData;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class JsonCompatibilityTests : TestsBase {

        private static JsonSerializerOptions GetOptions() {
            var result = new JsonSerializerOptions();
            result.Converters.AddDataCoreAdapterConverters();

            return result;
        }


        [TestMethod]
        public void LegacyTagValueStatusShouldBeDeserialized() {
            var json = @"{
    ""UtcSampleTime"": ""2021-10-26T08:37:13Z"",
    ""Status"": ""Bad"",
    ""Value"": {
        ""Type"": ""Double"",
        ""Value"": 3.1415927
    }
}";

            var value = JsonSerializer.Deserialize<TagValue>(json, GetOptions());
            Assert.AreEqual((StatusCode) StatusCodes.Bad, value.StatusCode);
        }


        [TestMethod]
        public void LegacyTagValueExtendedStatusShouldBeDeserialized() {
            var json = @"{
    ""UtcSampleTime"": ""2021-10-26T08:37:13Z"",
    ""Status"": ""Uncertain"",
    ""Value"": {
        ""Type"": ""Double"",
        ""Value"": 3.1415927
    },
    ""Notes"": ""Just a test""
}";

            var value = JsonSerializer.Deserialize<TagValueExtended>(json, GetOptions());
            Assert.AreEqual((StatusCode) StatusCodes.Uncertain, value.StatusCode);
        }


        [TestMethod]
        public void LegacyHealthCheckResultStatusShouldBeDeserialized() {
            var json = @"{
    ""DisplayName"": ""Unit Test"",
    ""Status"": ""Unhealthy""
}";

            var value = JsonSerializer.Deserialize<HealthCheckResult>(json, GetOptions());
            Assert.AreEqual((StatusCode) StatusCodes.Bad, value.StatusCode);
        }


        [TestMethod]
        public void LegacyWriteTagValueResultStatusShouldBeDeserialized() {
            var json = @"{
    ""CorrelationId"": ""12345"",
    ""TagId"": ""1"",
    ""Status"": ""Pending""
}";

            var value = JsonSerializer.Deserialize<WriteTagValueResult>(json, GetOptions());
            Assert.AreEqual((StatusCode) StatusCodes.Uncertain, value.StatusCode);
        }


        [TestMethod]
        public void LegacyWriteTagValueAnnotationResultStatusShouldBeDeserialized() {
            var json = @"{
    ""AnnotationId"": ""12345"",
    ""TagId"": ""1"",
    ""Status"": ""Success""
}";

            var value = JsonSerializer.Deserialize<WriteTagValueResult>(json, GetOptions());
            Assert.AreEqual((StatusCode) StatusCodes.Good, value.StatusCode);
        }


        [TestMethod]
        public void LegacyWriteEventMessageResultStatusShouldBeDeserialized() {
            var json = @"{
    ""CorrelationId"": ""12345"",
    ""Status"": ""Fail""
}";

            var value = JsonSerializer.Deserialize<WriteEventMessageResult>(json, GetOptions());
            Assert.AreEqual((StatusCode) StatusCodes.Bad, value.StatusCode);
        }

    }

}
