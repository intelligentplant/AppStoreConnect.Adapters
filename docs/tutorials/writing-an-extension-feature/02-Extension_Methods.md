# Tutorial - Writing an Adapter Extension Feature

_This is part 2 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Extension Methods

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-02)._

In the [previous chapter](01-Getting_Started.md), we created and registered an extension feature on our adapter, but the extension had a major flaw - there were no operations on the feature for us to call! In this chapter, we will add a simple request-response method to our extension feature, and bind it so that it gets registered as a discoverable operation.

Our operation will allow a caller to specify a `PingMessage` object and receive a corresponding `PongMessage` object in return. Add the following method into our `PingPongExtension` class:

```csharp
[ExtensionFeatureOperation(
    Description = "Responds to a ping message with a pong message",
    InputParameterDescription = "The ping message",
    OutputParameterDescription = "The pong message"
)]
public PongMessage Ping(PingMessage message) {
    if (message == null) {
        throw new ArgumentNullException(nameof(message));
    }

    return new PongMessage() {
        CorrelationId = message.CorrelationId
    };
}
```

Note that we annotate the method with an [ExtensionFeatureOperationAttribute](/src/DataCore.Adapter.Abstractions/Extensions/ExtensionFeatureOperationAttribute.cs) attribute. This is optional, but we include it so that the descriptor that is generated for the operation provides richer information. Next, update the `PingPongExtension` constructor as follows:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    BindInvoke<PingMessage, PongMessage>(Ping);
}
```

The constructor now makes a call to the `BindInvoke<TIn, TOut>` method to tell the base class that it should register our `Ping` method as an operation that can be called via the `Invoke` method on the `IAdapterExtensionFeature` interface. The `ExtensionFeatureOperationAttribute` annotation on the `Ping` method is used by the base class when constructing a descriptor for the operation. Compile and run the program again and we will see output similar to the following:

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
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Invoke/)
          - Description: Responds to a ping message with a pong message
```

Note that our `Ping` method is now listed as an invocable operation with its own URI that is derived from the extension URI. The `/Invoke/` section at the end of the URI indicates that the operation can be invoked via the `Invoke` method on the `IAdapterExtensionFeature` interface. The [ExtensionFeatureOperationDescriptor](/src/DataCore.Adapter.Core/Extensions/ExtensionFeatureOperationDescriptor.cs) object for the operation also includes an `OperationType` property that explicitly specifies how the operation is called, so that we don't have to infer this from the URI. 

The next step is for us to try invoking the operation.


### Invoking an Operation

When working with an in-process adapter, it is of course possible to retrieve the extension feature from the adapter's feature collection, and then cast it to the correct type and directly call a method on the extension feature type. However, this is not possible when trying to call an extension operation on an adapter that is running in an external process or on a remote server. This is where the methods defined on the `IAdapterExtensionFeature` interface come to the rescue.

In order to call our `Ping` method via a call to `Invoke`, we need to create a `PingMessage` instance, serialize this object to JSON, pass this to the `Invoke` method, receive a JSON-serialized response back, and then deserialize this response into a `PongMessage` instance. Fortunately, the [AdapterExtensionFeatureExtensions](/src/DataCore.Adapter/Extensions/AdapterExtensionFeatureExtensions.cs) class contains extension methods for `IAdapterExtensionFeature` that take care of the serialization and deserialization for us.

> [JSON.NET](https://www.newtonsoft.com/json) is used to serialize and deserialize inputs and outputs for extension operation calls. You must ensure that your request and response types can be serialized using the default settings defined by that library.

Add the following code to the end of the `using` block in the `Run` method in `Program.cs`:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
var correlationId = Guid.NewGuid().ToString();
var now = DateTime.UtcNow;
var pingMessage = new PingMessage() { CorrelationId = correlationId, UtcTime = now };
var pongMessage = await extensionFeature.Invoke<PingMessage, PongMessage>(
    context,
    new Uri("asc:extensions/tutorial/ping-pong/Ping/Invoke/"),
    pingMessage,
    cancellationToken
);

Console.WriteLine();
Console.WriteLine($"[INVOKE] Ping: {correlationId} @ {now:HH:mm:ss} UTC");
Console.WriteLine($"[INVOKE] Pong: {pongMessage.CorrelationId} @ {pongMessage.UtcTime:HH:mm:ss} UTC");
```

Compile and run the program again and the output will be similar to the following:

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
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Invoke/)
          - Description: Responds to a ping message with a pong message

[INVOKE] Ping: 780745b1-d437-490c-b349-a3f2f270806a @ 08:17:46 UTC
[INVOKE] Pong: 780745b1-d437-490c-b349-a3f2f270806a @ 08:17:46 UTC
```

This shows how easy it is to call an extension feature operation. If you do not have a strongly-typed definition for either the request or response type for the operation, you can also use an extension method that uses anonymous types instead, e.g.

```csharp
var pongMessageAnonymous = await extensionFeature.InvokeWithAnonymousResultType(
    context,
    new Uri("asc:extensions/tutorial/ping-pong/Ping/Invoke/"),
    new { CorrelationId = correlationId, UtcTime = now }, // Anonymous TIn type
    new { CorrelationId = string.Empty, UtcTime = DateTime.MinValue }, // Anonymous TOut type
    cancellationToken
);

Console.WriteLine();
Console.WriteLine($"[INVOKE] Ping: {correlationId} @ {now:HH:mm:ss} UTC");
Console.WriteLine($"[INVOKE] Pong: {pongMessageAnonymous.CorrelationId} @ {pongMessageAnonymous.UtcTime:HH:mm:ss} UTC");
```


## Next Steps

In the [next chapter](03-Streaming_Methods.md), we will add a new method to our extension feature that shows how an extension can use server-to-client streaming to return results back to a caller.
