using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.Json;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class JsonTests : TestsBase {

        private static JsonSerializerOptions GetOptions() {
            var result = new JsonSerializerOptions();
            result.Converters.AddDataCoreAdapterConverters();

            return result;
        }


        private void VariantRoundTripTest<T>(T value, JsonSerializerOptions options) {
            var variant = Variant.FromValue(value);
            var json = JsonSerializer.Serialize(variant, options);

            var deserialized = JsonSerializer.Deserialize<Variant>(json, options);
            Assert.AreEqual(variant.Type, deserialized.Type);

            var actualVal = (deserialized.Type == VariantType.Unknown) && deserialized.Value is JsonElement jsonElement
                ? JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), options)
                : deserialized.Value;

            if (variant.IsArray()) {
                Assert.IsTrue(((Array) variant.Value).Cast<object>().SequenceEqual(((Array) actualVal).Cast<object>()));
            }
            else {
                Assert.AreEqual(variant.Value, actualVal);
            }
        }


        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        [DataRow(true, false)]
        public void Variant_BooleanShouldRoundTrip(params bool[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow(byte.MinValue)]
        [DataRow(byte.MaxValue)]
        [DataRow(byte.MinValue, byte.MaxValue)]
        public void Variant_ByteShouldRoundTrip(params byte[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [TestMethod]
        public void Variant_DateTimeShouldRoundTrip() {
            var now = DateTime.UtcNow;
            var options = GetOptions();
            VariantRoundTripTest(now, options);
        }


        [DataTestMethod]
        [DataRow(double.MinValue)]
        [DataRow(double.MaxValue)]
        [DataRow(double.MinValue, double.MaxValue)]
        public void Variant_DoubleShouldRoundTrip(params double[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow(float.MinValue)]
        [DataRow(float.MaxValue)]
        [DataRow(float.MinValue, float.MaxValue)]
        public void Variant_FloatShouldRoundTrip(params float[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow(short.MinValue)]
        [DataRow(short.MaxValue)]
        [DataRow(short.MinValue, short.MaxValue)]
        public void Variant_Int16ShouldRoundTrip(params short[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow(int.MinValue)]
        [DataRow(int.MaxValue)]
        [DataRow(int.MinValue, int.MaxValue)]
        public void Variant_Int32ShouldRoundTrip(params int[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow(long.MinValue)]
        [DataRow(long.MaxValue)]
        [DataRow(long.MinValue, long.MaxValue)]
        public void Variant_Int64ShouldRoundTrip(params long[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow(sbyte.MinValue)]
        [DataRow(sbyte.MaxValue)]
        [DataRow(sbyte.MinValue, sbyte.MaxValue)]
        public void Variant_SByteShouldRoundTrip(params sbyte[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("TEST")]
        [DataRow("контрольная работа")] // "test" in Russian
        [DataRow("テスト")] // "test" in Japanese
        [DataRow("測試")] // "test" in Chinese
        [DataRow("اختبار")] // "test" in Arabic
        [DataRow("", " ", "TEST", "контрольная работа", "テスト", "測試", "اختبار")]
        public void Variant_StringShouldRoundTrip(params string[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [TestMethod]
        public void Variant_TimeSpanShouldRoundTrip() {
            var options = GetOptions();
            VariantRoundTripTest(TimeSpan.FromDays(1.234), options);
        }


        [DataTestMethod]
        [DataRow(ushort.MinValue)]
        [DataRow(ushort.MaxValue)]
        [DataRow(ushort.MinValue, ushort.MaxValue)]
        public void Variant_UInt16ShouldRoundTrip(params ushort[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow(uint.MinValue)]
        [DataRow(uint.MaxValue)]
        [DataRow(uint.MinValue, uint.MaxValue)]
        public void Variant_UInt32ShouldRoundTrip(params uint[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow(ulong.MinValue)]
        [DataRow(ulong.MaxValue)]
        [DataRow(ulong.MinValue, ulong.MaxValue)]
        public void Variant_UInt64ShouldRoundTrip(params ulong[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(values[0], options);
            }
            else {
                VariantRoundTripTest(values, options);
            }
        }


        [DataTestMethod]
        [DataRow("https://appstore.intelligentplant.com")]
        [DataRow("https://github.com/intelligentplant/AppStoreConnect.Adapters")]
        [DataRow("https://appstore.intelligentplant.com", "https://github.com/intelligentplant/AppStoreConnect.Adapters")]
        public void Variant_UrlShouldRoundTrip(params string[] values) {
            var options = GetOptions();
            if (values.Length == 1) {
                VariantRoundTripTest(new Uri(values[0], UriKind.Absolute), options);
            }
            else {
                VariantRoundTripTest(values.Select(x => new Uri(x, UriKind.Absolute)).ToArray(), options);
            }
        }


        [TestMethod]
        public void Variant_MultidimensionalArrayShouldRoundTrip() {
            var arr3d = new int[,,] { 
                { 
                    { 1, 2, 3 }, 
                    { 4, 5, 6 } 
                }, 
                { 
                    { 7, 8, 9 }, 
                    { 10, 11, 12 } 
                }, 
                { 
                    { 13, 14, 15 }, 
                    { 16, 17, 18 } 
                }, 
                { 
                    { 19, 20, 21 }, 
                    { 22, 23, 24 } 
                } 
            };

            VariantRoundTripTest(arr3d, GetOptions());

        }


        [TestMethod]
        public void AdapterDescriptor_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new AdapterDescriptor(
                "Id",
                "Name",
                "Description"
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<AdapterDescriptor>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Description, actual.Description);
        }


        [TestMethod]
        public void AdapterDescriptorExtended_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new AdapterDescriptorExtended(
                "Id",
                "Name",
                "Description",
                new [] {
                    "Feature1",
                    "Feature2"
                },
                new [] {
                    "Extension1",
                    "Extension2"
                },
                new [] {
                    new AdapterProperty("Property1", 100)
                },
                new AdapterTypeDescriptor(
                    new Uri("asc:unit-tests/json-tests/" + TestContext.TestName), 
                    TestContext.TestName, 
                    TestContext.TestName, 
                    "1.0.0",
                    new VendorInfo("", "")
                )
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<AdapterDescriptorExtended>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Description, actual.Description);

            Assert.AreEqual(expected.Features.Count(), actual.Features.Count());
            for (var i = 0; i < expected.Features.Count(); i++) {
                var expectedValue = expected.Features.ElementAt(i);
                var actualValue = actual.Features.ElementAt(i);
                Assert.AreEqual(expectedValue, actualValue);
            }

            Assert.AreEqual(expected.Extensions.Count(), actual.Extensions.Count());
            for (var i = 0; i < expected.Extensions.Count(); i++) {
                var expectedValue = expected.Extensions.ElementAt(i);
                var actualValue = actual.Extensions.ElementAt(i);
                Assert.AreEqual(expectedValue, actualValue);
            }

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }

            Assert.IsNotNull(actual.TypeDescriptor);
            Assert.AreEqual(expected.TypeDescriptor.Id, actual.TypeDescriptor.Id);
            Assert.AreEqual(expected.TypeDescriptor.Name, actual.TypeDescriptor.Name);
            Assert.AreEqual(expected.TypeDescriptor.Description, actual.TypeDescriptor.Description);
            Assert.IsNotNull(expected.TypeDescriptor.Vendor);
            Assert.AreEqual(expected.TypeDescriptor.Vendor.Name, actual.TypeDescriptor.Vendor.Name);
            Assert.AreEqual(expected.TypeDescriptor.Vendor.Url, actual.TypeDescriptor.Vendor.Url);
        }


        [TestMethod]
        public void AdapterProperty_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new AdapterProperty(
                "Name",
                "Value"
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<AdapterProperty>(json, options);

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Value, actual.Value);
        }


        [TestMethod]
        public void AssetModelNode_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new AssetModelNode(
                "Id",
                "Name",
                NodeType.Variable,
                null,
                "Description",
                "Parent",
                true,
                new DataReference("AdapterId1", "Id1"),
                new [] {
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<AssetModelNode>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.NodeType, actual.NodeType);
            Assert.AreEqual(expected.NodeSubType, actual.NodeSubType);
            Assert.AreEqual(expected.Description, actual.Description);
            Assert.AreEqual(expected.Parent, actual.Parent);
            Assert.AreEqual(expected.HasChildren, actual.HasChildren);
            Assert.IsNotNull(actual.DataReference);
            Assert.AreEqual(expected.DataReference.AdapterId, actual.DataReference.AdapterId);
            Assert.IsNotNull(actual.DataReference.Tag);
            Assert.AreEqual(expected.DataReference.Tag, actual.DataReference.Tag);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void DataFunctionDescriptor_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new DataFunctionDescriptor(
                "Id",
                "Name",
                "Description",
                DataFunctionSampleTimeType.StartTime,
                DataFunctionStatusType.Custom,
                new[] { 
                    AdapterProperty.Create("prop1", "value1", "description1")
                },
                new[] { 
                    "Alt_Id_1",
                    "Alt_Id_2"
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<DataFunctionDescriptor>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Description, actual.Description);
            Assert.AreEqual(expected.SampleTime, actual.SampleTime);
            Assert.AreEqual(expected.Status, actual.Status);
            foreach (var prop in expected.Properties) {
                var actualProp = actual.Properties.FirstOrDefault(p => p.Name.Equals(prop.Name));
                Assert.IsNotNull(actualProp);
                Assert.AreEqual(prop.Name, actualProp.Name);
                Assert.AreEqual(prop.Value, actualProp.Value);
                Assert.AreEqual(prop.Description, actualProp.Description);
            }
            foreach (var alias in expected.Aliases) {
                Assert.IsTrue(actual.Aliases.Contains(alias));
            }
        }


        [TestMethod]
        public void DigitalState_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new DigitalState(
                "Name",
                100
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<DigitalState>(json, options);

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Value, actual.Value);
        }


        [TestMethod]
        public void DigitalStateSet_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new DigitalStateSet(
                Guid.NewGuid().ToString(),
                "StateSetName",
                new [] {
                    new DigitalState(
                        "Name1",
                        100
                    ),
                    new DigitalState(
                        "Name2",
                        200
                    )
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<DigitalStateSet>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);

            Assert.AreEqual(expected.States.Count(), actual.States.Count());
            for (var i = 0; i < expected.States.Count(); i++) {
                var expectedValue = expected.States.ElementAt(i);
                var actualValue = actual.States.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void EventMessage_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new EventMessage(
                "Id",
                TestContext.TestName,
                DateTime.UtcNow,
                EventPriority.Medium,
                "Category",
                "Message",
                new[] {
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<EventMessage>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Topic, actual.Topic);
            Assert.AreEqual(expected.UtcEventTime, actual.UtcEventTime);
            Assert.AreEqual(expected.Priority, actual.Priority);
            Assert.AreEqual(expected.Category, actual.Category);
            Assert.AreEqual(expected.Message, actual.Message);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void EventMessageWithCursorPosition_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new EventMessageWithCursorPosition(
                "Id",
                TestContext.TestName,
                DateTime.UtcNow,
                EventPriority.Medium,
                "Category",
                "Message",
                new[] {
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value")
                },
                "CursorPosition"
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<EventMessageWithCursorPosition>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Topic, actual.Topic);
            Assert.AreEqual(expected.UtcEventTime, actual.UtcEventTime);
            Assert.AreEqual(expected.Priority, actual.Priority);
            Assert.AreEqual(expected.Category, actual.Category);
            Assert.AreEqual(expected.Message, actual.Message);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }

            Assert.AreEqual(expected.CursorPosition, actual.CursorPosition);
        }


        [TestMethod]
        public void HealthCheckResult_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new HealthCheckResult(
                "DisplayName",
                StatusCodes.Good,
                "Description",
                "Error",
                new Dictionary<string, string>() {
                    { "Data1", "Value1" },
                    { "Data2", "Value2" }
                },
                new [] {
                    new HealthCheckResult(
                        "InnerDisplayName",
                        StatusCodes.Good,
                        "InnerDescription",
                        "InnerError",
                        new Dictionary<string, string>() {
                            { "InnerData1", "Value1" },
                            { "InnerData2", "Value2" }
                        },
                        null
                    )
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<HealthCheckResult>(json, options);

            Assert.AreEqual(expected.DisplayName, actual.DisplayName);
            Assert.AreEqual(expected.StatusCode, actual.StatusCode);
            Assert.AreEqual(expected.Description, actual.Description);
            Assert.AreEqual(expected.Error, actual.Error);

            Assert.AreEqual(expected.Data.Count, actual.Data.Count);
            foreach (var item in expected.Data) {
                Assert.IsTrue(actual.Data.TryGetValue(item.Key, out var val));
                Assert.AreEqual(item.Value, val);
            }

            Assert.AreEqual(expected.InnerResults.Count(), actual.InnerResults.Count());
            for (var i = 0; i < expected.InnerResults.Count(); i++) {
                var expectedValue = expected.InnerResults.ElementAt(i);
                var actualValue = actual.InnerResults.ElementAt(i);

                Assert.AreEqual(expectedValue.DisplayName, actualValue.DisplayName);
                Assert.AreEqual(expectedValue.StatusCode, actualValue.StatusCode);
                Assert.AreEqual(expectedValue.Description, actualValue.Description);
                Assert.AreEqual(expectedValue.Error, actualValue.Error);

                Assert.AreEqual(expectedValue.Data.Count, actualValue.Data.Count);
                foreach (var item in expectedValue.Data) {
                    Assert.IsTrue(actualValue.Data.TryGetValue(item.Key, out var val));
                    Assert.AreEqual(item.Value, val);
                }
            }
        }


        [TestMethod]
        public void HostInfo_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new HostInfo(
               "Name",
               "Description",
               "1.0.0",
               new VendorInfo("Vendor", "https://some-vendor.com"),
               new[] {
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value")
               }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<HostInfo>(json, options);

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Description, actual.Description);
            Assert.AreEqual(expected.Version, actual.Version);
            Assert.AreEqual(expected.Vendor.Name, actual.Vendor.Name);
            Assert.AreEqual(expected.Vendor.Url, actual.Vendor.Url);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void ProcessedTagValueQueryResult_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new ProcessedTagValueQueryResult(
               "Id",
               "Name",
               new TagValueExtended(
                   DateTime.UtcNow, 
                   Variant.FromValue(100),
                   StatusCodes.Good, 
                   "Units", 
                   "Notes", 
                   "Error",
                   new[] {
                        AdapterProperty.Create("Prop1", 100),
                        AdapterProperty.Create("Prop2", "Value"),
                        AdapterProperty.Create(WellKnownProperties.TagValue.DisplayValue, "OPEN")
                   }
                ),
               "DataFunction"
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<ProcessedTagValueQueryResult>(json, options);

            Assert.AreEqual(expected.TagId, actual.TagId);
            Assert.AreEqual(expected.TagName, actual.TagName);
            Assert.AreEqual(expected.DataFunction, actual.DataFunction);
            Assert.AreEqual(expected.Value.UtcSampleTime, actual.Value.UtcSampleTime);
            Assert.AreEqual(expected.Value.StatusCode, actual.Value.StatusCode);
            Assert.AreEqual(expected.Value.Units, actual.Value.Units);
            Assert.AreEqual(expected.Value.Notes, actual.Value.Notes);
            Assert.AreEqual(expected.Value.Value, actual.Value.Value);

            Assert.AreEqual(expected.Value.Properties.Count(), actual.Value.Properties.Count());
            for (var i = 0; i < expected.Value.Properties.Count(); i++) {
                var expectedValue = expected.Value.Properties.ElementAt(i);
                var actualValue = actual.Value.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagDefinition_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = TagDefinitionBuilder
                .Create()
                .WithId("Id")
                .WithName("Name")
                .WithDescription("Description")
                .WithUnits("Units")
                .WithDataType(VariantType.Int32)
                .WithDigitalStates(
                    DigitalState.Create("State1", 100),
                    DigitalState.Create("State2", 200)
                )
                .WithSupportsReadSnapshotValues()
                .WithSupportsReadRawValues()
                .WithSupportsReadProcessedValues(DefaultDataFunctions.Average, DefaultDataFunctions.Interpolate)
                .WithProperties(
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value")
                )
                .WithLabels("Label1", "Label2")
                .Build();

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagDefinition>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Description, actual.Description);
            Assert.AreEqual(expected.Units, actual.Units);
            Assert.AreEqual(expected.DataType, actual.DataType);

            Assert.AreEqual(expected.States.Count(), actual.States.Count());
            for (var i = 0; i < expected.States.Count(); i++) {
                var expectedValue = expected.States.ElementAt(i);
                var actualValue = actual.States.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }

            Assert.AreEqual(expected.SupportedFeatures.Count(), actual.SupportedFeatures.Count());
            for (var i = 0; i < expected.SupportedFeatures.Count(); i++) {
                var expectedValue = expected.SupportedFeatures.ElementAt(i);
                var actualValue = actual.SupportedFeatures.ElementAt(i);

                Assert.AreEqual(expectedValue, actualValue);
            }

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }

            Assert.AreEqual(expected.Labels.Count(), actual.Labels.Count());
            for (var i = 0; i < expected.Labels.Count(); i++) {
                var expectedValue = expected.Labels.ElementAt(i);
                var actualValue = actual.Labels.ElementAt(i);

                Assert.AreEqual(expectedValue, actualValue);
            }
        }


        [TestMethod]
        public void TagIdentifier_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagIdentifier(
               "Id",
               "Name"
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagDefinition>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
        }


        [TestMethod]
        public void TagSummary_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagSummary(
                "Id",
                "Name",
                "Description",
                "Units",
                VariantType.Double
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagSummary>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Description, actual.Description);
            Assert.AreEqual(expected.Units, actual.Units);
            Assert.AreEqual(expected.DataType, actual.DataType);
        }


        [TestMethod]
        public void TagValueAnnotation_WithClosedTimeRange_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagValueAnnotation(
                AnnotationType.TimeRange,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                "Value",
                "Description",
                new[] {
                   AdapterProperty.Create("Prop1", 100),
                   AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueAnnotation>(json, options);

            Assert.AreEqual(expected.AnnotationType, actual.AnnotationType);
            Assert.AreEqual(expected.UtcStartTime, actual.UtcStartTime);
            Assert.AreEqual(expected.UtcEndTime, actual.UtcEndTime);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Description, actual.Description);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagValueAnnotation_WithOpenTimeRange_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagValueAnnotation(
                AnnotationType.TimeRange,
                DateTime.UtcNow.AddDays(-1),
                null,
                "Value",
                "Description",
                new[] {
                   AdapterProperty.Create("Prop1", 100),
                   AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueAnnotation>(json, options);

            Assert.AreEqual(expected.AnnotationType, actual.AnnotationType);
            Assert.AreEqual(expected.UtcStartTime, actual.UtcStartTime);
            Assert.AreEqual(expected.UtcEndTime, actual.UtcEndTime);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Description, actual.Description);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagValueAnnotation_WithInstantaneousTime_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagValueAnnotation(
                AnnotationType.Instantaneous,
                DateTime.UtcNow.AddDays(-1),
                null,
                "Value",
                "Description",
                new[] {
                   AdapterProperty.Create("Prop1", 100),
                   AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueAnnotation>(json, options);

            Assert.AreEqual(expected.AnnotationType, actual.AnnotationType);
            Assert.AreEqual(expected.UtcStartTime, actual.UtcStartTime);
            Assert.AreEqual(expected.UtcEndTime, actual.UtcEndTime);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Description, actual.Description);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagValueAnnotationExtended_WithClosedTimeRange_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagValueAnnotationExtended(
                "Id",
                AnnotationType.TimeRange,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow,
                "Value",
                "Description",
                new[] {
                   AdapterProperty.Create("Prop1", 100),
                   AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueAnnotationExtended>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.AnnotationType, actual.AnnotationType);
            Assert.AreEqual(expected.UtcStartTime, actual.UtcStartTime);
            Assert.AreEqual(expected.UtcEndTime, actual.UtcEndTime);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Description, actual.Description);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagValueAnnotationExtended_WithOpenTimeRange_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagValueAnnotationExtended(
                "Id",
                AnnotationType.TimeRange,
                DateTime.UtcNow.AddDays(-1),
                null,
                "Value",
                "Description",
                new[] {
                   AdapterProperty.Create("Prop1", 100),
                   AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueAnnotationExtended>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.AnnotationType, actual.AnnotationType);
            Assert.AreEqual(expected.UtcStartTime, actual.UtcStartTime);
            Assert.AreEqual(expected.UtcEndTime, actual.UtcEndTime);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Description, actual.Description);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagValueAnnotationExtended_WithInstantaneousTime_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagValueAnnotationExtended(
                "Id",
                AnnotationType.Instantaneous,
                DateTime.UtcNow.AddDays(-1),
                null,
                "Value",
                "Description",
                new[] {
                   AdapterProperty.Create("Prop1", 100),
                   AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueAnnotationExtended>(json, options);

            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.AnnotationType, actual.AnnotationType);
            Assert.AreEqual(expected.UtcStartTime, actual.UtcStartTime);
            Assert.AreEqual(expected.UtcEndTime, actual.UtcEndTime);
            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.Description, actual.Description);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagValueAnnotationQueryResult_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagValueAnnotationQueryResult(
                "Id",
                "Name",
                new TagValueAnnotationExtended(
                    "Id",
                    AnnotationType.Instantaneous,
                    DateTime.UtcNow.AddDays(-1),
                    null,
                    "Value",
                    "Description",
                    new[] {
                       AdapterProperty.Create("Prop1", 100),
                       AdapterProperty.Create("Prop2", "Value")
                    }
                )
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueAnnotationQueryResult>(json, options);

            Assert.AreEqual(expected.TagId, actual.TagId);
            Assert.AreEqual(expected.TagName, actual.TagName);
            Assert.AreEqual(expected.Annotation.Id, actual.Annotation.Id);
            Assert.AreEqual(expected.Annotation.AnnotationType, actual.Annotation.AnnotationType);
            Assert.AreEqual(expected.Annotation.UtcStartTime, actual.Annotation.UtcStartTime);
            Assert.AreEqual(expected.Annotation.UtcEndTime, actual.Annotation.UtcEndTime);
            Assert.AreEqual(expected.Annotation.Value, actual.Annotation.Value);
            Assert.AreEqual(expected.Annotation.Description, actual.Annotation.Description);

            Assert.AreEqual(expected.Annotation.Properties.Count(), actual.Annotation.Properties.Count());
            for (var i = 0; i < expected.Annotation.Properties.Count(); i++) {
                var expectedValue = expected.Annotation.Properties.ElementAt(i);
                var actualValue = actual.Annotation.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagValue_ShouldRoundTrip() {
            var options = GetOptions();
            var expected =
            new TagValue(
                DateTime.UtcNow,
                Variant.FromValue(100),
                StatusCodes.Good,
                "Units"
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValue>(json, options);

            Assert.AreEqual(expected.UtcSampleTime, actual.UtcSampleTime);
            Assert.AreEqual(expected.StatusCode, actual.StatusCode);
            Assert.AreEqual(expected.Units, actual.Units);

            Assert.AreEqual(expected.Value, actual.Value);
        }


        [TestMethod]
        public void TagValueExtended_ShouldRoundTrip() {
            var options = GetOptions();
            var expected =
            new TagValueExtended(
                DateTime.UtcNow,
                Variant.FromValue(100),
                StatusCodes.Good,
                "Units",
                "Notes",
                "Error",
                new[] {
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value"),
                    AdapterProperty.Create(WellKnownProperties.TagValue.DisplayValue, "OPEN")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueExtended>(json, options);

            Assert.AreEqual(expected.UtcSampleTime, actual.UtcSampleTime);
            Assert.AreEqual(expected.StatusCode, actual.StatusCode);
            Assert.AreEqual(expected.Units, actual.Units);
            Assert.AreEqual(expected.Notes, actual.Notes);

            Assert.AreEqual(expected.Value, actual.Value);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void TagValueQueryResult_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new TagValueQueryResult(
               "Id",
               "Name",
               new TagValueExtended(
                   DateTime.UtcNow,
                   Variant.FromValue(100),
                   StatusCodes.Good,
                   "Units",
                   "Notes",
                   "Error",
                   new[] {
                        AdapterProperty.Create("Prop1", 100),
                        AdapterProperty.Create("Prop2", "Value"),
                        AdapterProperty.Create(WellKnownProperties.TagValue.DisplayValue, "OPEN")
                   }
                )
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<TagValueQueryResult>(json, options);

            Assert.AreEqual(expected.TagId, actual.TagId);
            Assert.AreEqual(expected.TagName, actual.TagName);
            Assert.AreEqual(expected.Value.UtcSampleTime, actual.Value.UtcSampleTime);
            Assert.AreEqual(expected.Value.StatusCode, actual.Value.StatusCode);
            Assert.AreEqual(expected.Value.Units, actual.Value.Units);
            Assert.AreEqual(expected.Value.Notes, actual.Value.Notes);

            Assert.AreEqual(expected.Value.Value, actual.Value.Value);

            Assert.AreEqual(expected.Value.Properties.Count(), actual.Value.Properties.Count());
            for (var i = 0; i < expected.Value.Properties.Count(); i++) {
                var expectedValue = expected.Value.Properties.ElementAt(i);
                var actualValue = actual.Value.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void VendorInfo_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new VendorInfo("Vendor", "https://some-vendor.com");

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<VendorInfo>(json, options);

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Url, actual.Url);
        }


        [TestMethod]
        public void WriteEventMessageResult_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new WriteEventMessageResult(
                "CorrelationId",
                StatusCodes.Good,
                "Notes",
                new[] {
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<WriteEventMessageResult>(json, options);

            Assert.AreEqual(expected.CorrelationId, actual.CorrelationId);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.Notes, actual.Notes);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void WriteTagValueAnnotationResult_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new WriteTagValueAnnotationResult(
                "TagId",
                "AnnotationId",
                StatusCodes.Good,
                "Notes",
                new[] {
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<WriteTagValueAnnotationResult>(json, options);

            Assert.AreEqual(expected.TagId, actual.TagId);
            Assert.AreEqual(expected.AnnotationId, actual.AnnotationId);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.Notes, actual.Notes);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        [TestMethod]
        public void WriteTagValueResult_ShouldRoundTrip() {
            var options = GetOptions();
            var expected = new WriteTagValueResult(
                "CorrelationId",
                "TagId",
                StatusCodes.Good,
                "Notes",
                new[] {
                    AdapterProperty.Create("Prop1", 100),
                    AdapterProperty.Create("Prop2", "Value")
                }
            );

            var json = JsonSerializer.Serialize(expected, options);
            var actual = JsonSerializer.Deserialize<WriteTagValueResult>(json, options);

            Assert.AreEqual(expected.CorrelationId, actual.CorrelationId);
            Assert.AreEqual(expected.TagId, actual.TagId);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.Notes, actual.Notes);

            Assert.AreEqual(expected.Properties.Count(), actual.Properties.Count());
            for (var i = 0; i < expected.Properties.Count(); i++) {
                var expectedValue = expected.Properties.ElementAt(i);
                var actualValue = actual.Properties.ElementAt(i);

                Assert.AreEqual(expectedValue.Name, actualValue.Name);
                Assert.AreEqual(expectedValue.Value, actualValue.Value);
            }
        }


        private class VariantTestClass {

            public string TestName { get; set; }

            public DateTime UtcTimestamp { get; set; }


            public override int GetHashCode() {
#if NET48
                return HashGenerator.Combine(TestName, UtcTimestamp);
#else
                return HashCode.Combine(TestName, UtcTimestamp);
#endif
            }


            public override bool Equals(object obj) {
                if (!(obj is VariantTestClass s)) {
                    return false;
                }
                return string.Equals(TestName, s.TestName) && UtcTimestamp.Equals(s.UtcTimestamp);
            }

        }

    }

}
