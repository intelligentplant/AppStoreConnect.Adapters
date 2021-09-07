# Tutorial - Writing an Adapter Extension Feature

_This is part 5 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Implementing Multiple Extensions

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-05)._

In the [previous chapter](04-Duplex_Streaming_Methods.md), we implemented a duplex streaming operation that allowed us to asynchronously push values in and out of an operation until the caller cancelled their subscription. In this chapter, we will learn how to define extensions in separate interface definitions to our implementation class, and implement multiple extensions on the same type.

Our new extension will be a simple temperature converter, with operations for converting from degrees Celsius to degrees Fahrenheit, and vice versa. To get started, we will create a new file called `ITemperatureConverter.cs` and add our extension feature definition to the file:

```csharp
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
```

Note that, in addition to the interface itself, we have included a class that returns metadata about the extension feature's operations, and we have annotated our interface methods with `[ExtensionFeatureOperation]` attributes. These attributes allow us to define provider methods for retrieving metadata about the operations so that we do not have to provide this information when we bind the operations.

Note as well that, when defining the metadata about the input and output parameters, we are defining our JSON schemas manually using the `SchemaBuilder` class from the [JsonSchema.Net.Generation](https://www.nuget.org/packages/JsonSchema.Net.Generation/) library. The `ToJsonElement()` extension method from the `DataCore.Adapter.Json` namespace automatically serializes the schema to the format that we require. It is useful to manually define schemas when our input and/or output types are primitive types that we cannot annotate with attributes to customise automatically-generated schemas.

Next, we will update our `PingPongExtension` class to implement our new interface:

```csharp
[ExtensionFeature(
    ExtensionUri,
    Name = "Ping Pong",
    Description = "Example extension feature."
)]
public class PingPongExtension : AdapterExtensionFeature, ITemperatureConverter {

    // -- Existing implementation removed for brevity --

    public double CtoF(double degC) {
        return (degC * 1.8) + 32;
    }

    public double FtoC(double degF) {
        return (degF - 32) / 1.8;
    }
}
```

Next, we bind the `CtoF` and `FtoC` methods in our constructor, as we did with the other methods on the original feature:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    // -- Existing bindings removed for brevity --

    // ITemperatureConverter bindings
    BindInvoke<ITemperatureConverter, double, double>(CtoF);
    BindInvoke<ITemperatureConverter, double, double>(FtoC);
}
```

Note that our new `BindInvoke` calls specify the type of our new extension feature (`ITemperatureConverter`) rather than the `PingPongExtension` type. This is required so that the operation IDs generated for the bindings use the feature ID of our `ITemperatureConverter` feature. Note also that, because we have defined our operation metadata via `[ExtensionFeatureOperation]` annotations on our interface methods, our binding call is much more concise!

If you run and compile the program, you will see that the new extension and its registered operations are now visible in the adapter summary:

```
Adapter Summary:

{
  "id": "example",
  "name": "Example Adapter",
  "description": "Example adapter with an extension feature, built using the tutorial on GitHub",
  "properties": {},
  "features": [
    "asc:features/diagnostics/health-check/"
  ],
  "extensions": {
    "asc:extensions/tutorial/ping-pong/": {
      // Removed for brevity
    },
    "asc:extensions/tutorial/temperature-converter/": {
      "name": "Temperature Converter",
      "description": "Converts Celsius to Fahrenheit and vice versa.",
      "operations": {
        "asc:extensions/tutorial/temperature-converter/invoke/FtoC/": {
          "operationType": "Invoke",
          "name": "FtoC",
          "description": "Converts a temperature in Fahrenheit to Celsius",
          "requestSchema": {
            "type": "number",
            "description": "The temperature in degrees Fahrenheit"
          },
          "responseSchema": {
            "type": "number",
            "description": "The temperature in degrees Celsius"
          }
        },
        "asc:extensions/tutorial/temperature-converter/invoke/CtoF/": {
          "operationType": "Invoke",
          "name": "CtoF",
          "description": "Converts a temperature in Celsius to Fahrenheit",
          "requestSchema": {
            "type": "number",
            "description": "The temperature in degrees Celsius"
          },
          "responseSchema": {
            "type": "number",
            "description": "The temperature in degrees Fahrenheit"
          }
        }
      }
    }
  }
}
```

Our final step is to test the execution of our new operations. Replace any existing code in `Runner.cs` for calling extension operations with the following:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/temperature-converter/");
Console.WriteLine();

var degC = 40d;
var degF = await extensionFeature.Invoke<double, double>(
    context,
    new Uri("asc:extensions/tutorial/temperature-converter/invoke/CtoF/"),
    degC,
    null,
    cancellationToken
);

Console.WriteLine($"{degC:0.#} Celsius is {degF:0.#} Fahrenheit");

degF = 60d;
degC = await extensionFeature.Invoke<double, double>(
    context,
    new Uri("asc:extensions/tutorial/temperature-converter/invoke/FtoC/"),
    degF,
    null,
    cancellationToken
);

Console.WriteLine($"{degF:0.#} Fahrenheit is {degC:0.#} Celsius");
```

When you compile and run the program again, you will see the following output:

```
-- Adapter summary removed for brevity --

40 Celsius is 104 Fahrenheit
60 Fahrenheit is 15.6 Celsius
```


## Next Steps

This is the last part of this tutorial. We recommend that you explore the `AdapterExtensionFeature` base class to get a complete understanding of how extension features are implemented. Note that you can also override various `protected` methods on the base class to take more direct control over the feature's behaviour (for example, to register or handle bespoke operations that do not use one of the `BindXXX` methods to register the operation).
