using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataCore.Adapter.NewtonsoftJson;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class NewtonsoftJsonTests : TestsBase {

        private static JsonSerializerSettings GetSerializerSettings() {
            var result = new JsonSerializerSettings();
            result.AddDataCoreAdapterConverters();

            return result;
        }


        [TestMethod]
        public void ShouldSerializeJsonElement() {
            var settings = GetSerializerSettings();

            var expected = Json.JsonElementExtensions.ToJsonElement(new { 
                A = 1,
                B = "Two",
                C = DateTime.UtcNow
            });

            var json = JsonConvert.SerializeObject(expected, settings);

            var actual = JsonConvert.DeserializeObject<System.Text.Json.JsonElement>(json, settings);

            Assert.AreEqual(expected.ToString(), actual.ToString());
        }


        [TestMethod]
        public void ShouldSerializeNullableJsonElement() {
            var settings = GetSerializerSettings();

            var expected = Json.JsonElementExtensions.ToJsonElement(new {
                A = 1,
                B = "Two",
                C = DateTime.UtcNow
            });

            var json = JsonConvert.SerializeObject(expected, settings);

            var actual = JsonConvert.DeserializeObject<System.Text.Json.JsonElement?>(json, settings);

            Assert.AreEqual(expected.ToString(), actual.Value.ToString());
        }


        [TestMethod]
        public void ShouldSerializeEmbeddedJsonElement() {
            var settings = GetSerializerSettings();

            var expected = new TestModel() {
                Json = Json.JsonElementExtensions.ToJsonElement(new {
                    A = 1,
                    B = "Two",
                    C = DateTime.UtcNow
                })
            };

            var json = JsonConvert.SerializeObject(expected, settings);

            var actual = JsonConvert.DeserializeObject<TestModel>(json, settings);

            Assert.AreEqual(expected.Json.ToString(), actual.Json.ToString());
        }


        public class TestModel { 
        
            public System.Text.Json.JsonElement? Json { get; set; }

        }

    }

}
