using DataCore.Adapter.Extensions;
using DataCore.Adapter.Json;

using Json.Schema;

namespace MyAdapter {

    [ExtensionFeature(
        "tutorial/temperature-converter/",
        Name = "Temperature Converter",
        Description = "Converts Celsius to Fahrenheit and vice versa."
    )]
    public interface ITemperatureConverter : IAdapterExtensionFeature {

        [ExtensionFeatureOperation(typeof(TemperatureConverterMetadata), nameof(TemperatureConverterMetadata.GetCtoFMetadata))]
        double CtoF(double degC);

        [ExtensionFeatureOperation(typeof(TemperatureConverterMetadata), nameof(TemperatureConverterMetadata.GetFtoCMetadata))]
        double FtoC(double degF);

    }


    public static class TemperatureConverterMetadata {

        public static ExtensionFeatureOperationDescriptorPartial GetCtoFMetadata() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Description = "Converts a temperature in Celsius to Fahrenheit",
                RequestSchema = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Number)
                    .Description("The temperature in degrees Celsius")
                    .Build()
                    .ToJsonElement(),
                ResponseSchema = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Number)
                    .Description("The temperature in degrees Fahrenheit")
                    .Build()
                    .ToJsonElement()
            };
        }


        public static ExtensionFeatureOperationDescriptorPartial GetFtoCMetadata() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Description = "Converts a temperature in Fahrenheit to Celsius",
                RequestSchema = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Number)
                    .Description("The temperature in degrees Fahrenheit")
                    .Build()
                    .ToJsonElement(),
                ResponseSchema = new JsonSchemaBuilder()
                    .Type(SchemaValueType.Number)
                    .Description("The temperature in degrees Celsius")
                    .Build()
                    .ToJsonElement()
            };
        }

    }

}
