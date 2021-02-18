# Tutorial - Writing an Adapter Extension Feature

_This is part 5 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Implementing Multiple Extensions

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-05)._

In the [previous chapter](04-Duplex_Streaming_Methods.md), we implemented a duplex streaming operation that allowed us to asynchronously push values in and out of an operation until the caller cancelled their subscription. In this chapter, we will learn how to define extensions in separate interface definitions to our implementation class, and implement multiple extensions on the same type.

Our new extension will be a simple temperature converter, with operations for converting from degrees Celsius to degrees Fahrenheit, and vice versa. To get started, we will create a new file called `ITemperatureConverter.cs` and add our extension feature definition to the file:

```csharp
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
```

Note that, unlike with the `PingPongExtension` we have been building, we can also define extension features and associated metadata directly on an interface. This saves us from having to re-declare the same information in our implementation class. Next, we will update our `PingPongExtension` class to implement our new interface:

```csharp
[ExtensionFeature(
    ExtensionUri,
    Name = "Ping Pong",
    Description = "Example extension feature."
)]
public class PingPongExtension : AdapterExtensionFeature, ITemperatureConverter {

    // -- existing implementation removed for brevity --

    public double CtoF(double degC) {
        return (degC * 1.8) + 32;
    }

    public double FtoC(double degF) {
        return (degF - 32) / 1.8;
    }
}
```

Since the metadata for the temperature converter is defined in `ITemperatureConverter`, we don't have to add any additional annotations. Next, we bind the `CtoF` and `FtoC` methods in our constructor, as we did with the other methods on the original feature:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    BindInvoke<PingMessage, PongMessage>(Ping);
    BindStream<PingMessage, PongMessage>(Ping);
    BindDuplexStream<PingMessage, PongMessage>(Ping);

    // ITemperatureConverter bindings
    BindInvoke<double, double>(CtoF, 24, 75.2);
    BindInvoke<double, double>(FtoC, 100, 37.8);
}
```

Note that our new `BindInvoke` calls also include example values for the input and output parameters for the methods; these values will be included in the operation descriptors that are generated for the new methods. If you run and compile the program, you will see that the new extension and its registered operations are now visible in the adapter summary:

```
[example]
  Name: Example Adapter
  Description: Example adapter with an extension feature, built using the tutorial on GitHub
  Properties:
  Features:
    - asc:features/diagnostics/health-check/
  Extensions:
    - asc:extensions/tutorial/ping-pong/
      - Name: Ping Pong
      - Description: Example extension feature.
      - Operations:
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Stream/)
          - Description: Responds to a ping message with a pong message every second until the call is cancelled
        - Ping (asc:extensions/tutorial/ping-pong/Ping/DuplexStream/)
          - Description: Responds to each ping message in an incoming stream with a pong message
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Invoke/)
          - Description: Responds to a ping message with a pong message
    - asc:extensions/tutorial/temperature-converter/
      - Name: Temperature Converter
      - Description: Converts Celsius to Fahrenheit and vice versa.
      - Operations:
        - CtoF (asc:extensions/tutorial/temperature-converter/CtoF/Invoke/)
          - Description: Converts a temperature from Celsius to Fahrenheit
        - FtoC (asc:extensions/tutorial/temperature-converter/FtoC/Invoke/)
          - Description: Converts a temperature from Fahrenheit to Celsius
```

Our final step is to test the execution of our new operations. Replace any existing code in `Runner.cs` for calling extension operations with the following:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/temperature-converter/");
Console.WriteLine();

var degC = 40d;
var degF = await extensionFeature.Invoke<double, double>(
    context,
    new Uri("asc:extensions/tutorial/temperature-converter/CtoF/Invoke/"),
    degC,
    cancellationToken
);

Console.WriteLine($"{degC:0.#} Celsius is {degF:0.#} Fahrenheit");

degF = 60d;
degC = await extensionFeature.Invoke<double, double>(
    context,
    new Uri("asc:extensions/tutorial/temperature-converter/FtoC/Invoke/"),
    degF,
    cancellationToken
);

Console.WriteLine($"{degF:0.#} Fahrenheit is {degC:0.#} Celsius");
```

When you compile and run the program again, you will see the following output:

```
[example]
  Name: Example Adapter
  Description: Example adapter with an extension feature, built using the tutorial on GitHub
  Properties:
  Features:
    - asc:features/diagnostics/health-check/
  Extensions:
    - asc:extensions/tutorial/ping-pong/
      - Name: Ping Pong
      - Description: Example extension feature.
      - Operations:
        - Ping (asc:extensions/tutorial/ping-pong/Ping/DuplexStream/)
          - Description: Responds to each ping message in an incoming stream with a pong message
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Invoke/)
          - Description: Responds to a ping message with a pong message
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Stream/)
          - Description: Responds to a ping message with a pong message every second until the call is cancelled
    - asc:extensions/tutorial/temperature-converter/
      - Name: Temperature Converter
      - Description: Converts Celsius to Fahrenheit and vice versa.
      - Operations:
        - CtoF (asc:extensions/tutorial/temperature-converter/CtoF/Invoke/)
          - Description: Converts a temperature from Celsius to Fahrenheit
        - FtoC (asc:extensions/tutorial/temperature-converter/FtoC/Invoke/)
          - Description: Converts a temperature from Fahrenheit to Celsius

40 Celsius is 104 Fahrenheit
60 Fahrenheit is 15.6 Celsius
```


## Next Steps

This is the last part of this tutorial. We recommend that you explore the various `BindInvoke`, `BindStream` and `BindDuplexStream` overloads to discover the different method signatures that can be automatically registered as extension feature operations. Note that you can also override various `protected` methods on the base class to take more direct control over the feature's behaviour (for example, to register or handle bespoke operations that do not use one of the `BindXXX` methods to register the operation).
