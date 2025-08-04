using System;

using DataCore.Adapter.NewtonsoftJson;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class NewtonsoftJsonTests : TestsBase {

        private JsonSerializerSettings GetOptions() {
            var options = new JsonSerializerSettings();
            options.UseDataCoreAdapterDefaults();
            return options;
        }



        [TestMethod]
        [DataRow(@"""2024-05-18T06:00:00.0000000Z""", "2024-05-18T06:00:00.0000000Z")]
        [DataRow(@"""2024-05-18T06:00:00.1234567+05:30""", "2024-05-18T00:30:00.1234567Z")]
        [DataRow(@"""2024-05-18T19:45:27.7654321-09:00""", "2024-05-19T04:45:27.7654321Z")]
        public void DateTimeShouldBeConvertedToUtcOnDeserialize(string json, string expectedUtcDate) {
            var options = GetOptions();

            var actual = JsonConvert.DeserializeObject<DateTime>(json, options);
            var expected = DateTimeOffset.Parse(expectedUtcDate).UtcDateTime;

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(DateTimeKind.Utc, expected.Kind);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
        }


        [TestMethod]
        [DataRow("2024-05-18T19:45:27.1234567", DateTimeKind.Utc)]
        [DataRow("2024-05-18T19:45:27.1234567", DateTimeKind.Local)]
        [DataRow("2024-05-18T19:45:27.1234567", DateTimeKind.Unspecified)]
        public void DateTimeShouldBeConvertedToUtcOnSerialize(string dateString, DateTimeKind kind) {
            var options = GetOptions();

            var date = DateTime.SpecifyKind(DateTime.Parse(dateString), kind);
            var expectedJson = $@"""{date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}""";

            var actual = JsonConvert.SerializeObject(date, options);
            Assert.AreEqual(expectedJson, actual);
        }

    }

}
