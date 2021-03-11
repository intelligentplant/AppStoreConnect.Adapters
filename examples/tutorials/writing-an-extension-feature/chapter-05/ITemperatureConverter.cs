
using DataCore.Adapter.Extensions;

namespace MyAdapter {

    [ExtensionFeature(
        "tutorial/temperature-converter/",
        Name = "Temperature Converter",
        Description = "Converts Celsius to Fahrenheit and vice versa."
    )]
    public interface ITemperatureConverter : IAdapterExtensionFeature {

        double CtoF(double degC);

        double FtoC(double degF);

    }

}
