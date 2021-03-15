
using DataCore.Adapter;
using DataCore.Adapter.Extensions;

namespace MyAdapter {

    [ExtensionFeature(
        "tutorial/temperature-converter/",
        Name = "Temperature Converter",
        Description = "Converts Celsius to Fahrenheit and vice versa."
    )]
    public interface ITemperatureConverter : IAdapterExtensionFeature {

        [ExtensionFeatureOperation(typeof(TemperatureConverterMetadata), nameof(TemperatureConverterMetadata.GetCtoFMetadata))]
        double CtoF(IAdapterCallContext context, double degC);

        [ExtensionFeatureOperation(typeof(TemperatureConverterMetadata), nameof(TemperatureConverterMetadata.GetFtoCMetadata))]
        double FtoC(IAdapterCallContext context, double degF);

    }


    public static class TemperatureConverterMetadata { 
    
        public static ExtensionFeatureOperationDescriptorPartial GetCtoFMetadata() {
            return new ExtensionFeatureOperationDescriptorPartial() { 
                Description = "Converts a temperature in Celsius to Fahrenheit",
                Inputs = new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = DataCore.Adapter.Common.VariantType.Double,
                        Description = "The temperature in Celsius."
                    }
                },
                Outputs = new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = DataCore.Adapter.Common.VariantType.Double,
                        Description = "The temperature in Fahrenheit."
                    }
                }
            };
        }


        public static ExtensionFeatureOperationDescriptorPartial GetFtoCMetadata() {
            return new ExtensionFeatureOperationDescriptorPartial() {
                Description = "Converts a temperature in Fahrenheit to Celsius",
                Inputs = new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = DataCore.Adapter.Common.VariantType.Double,
                        Description = "The temperature in Fahrenheit."
                    }
                },
                Outputs = new[] {
                    new ExtensionFeatureOperationParameterDescriptor() {
                        Ordinal = 0,
                        VariantType = DataCore.Adapter.Common.VariantType.Double,
                        Description = "The temperature in Celsius."
                    }
                }
            };
        }

    }

}
