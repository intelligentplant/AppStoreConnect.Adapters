
using DataCore.Adapter.Extensions;

namespace MyAdapter {

    [ExtensionFeature(
        "tutorial/temperature-converter/",
        Name = "Temperature Converter",
        Description = "Converts Celsius to Fahrenheit and vice versa."
    )]
    public interface ITemperatureConverter : IAdapterExtensionFeature {

        [ExtensionFeatureOperation(
            Description = "Converts a temperature from Celsius to Fahrenheit",
            InputParameterDescription = "The temperature in Celsius",
            OutputParameterDescription = "The temperature in Fahrenheit"
        )]
        double CtoF(double degC);

        [ExtensionFeatureOperation(
            Description = "Converts a temperature from Fahrenheit to Celsius",
            InputParameterDescription = "The temperature in Fahrenheit",
            OutputParameterDescription = "The temperature in Celsius"
        )]
        double FtoC(double degF);

    }

}
