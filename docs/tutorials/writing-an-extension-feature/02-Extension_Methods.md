# Tutorial - Writing an Adapter Extension Feature

_This is part 2 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Extension Methods

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-02)._

In the [previous chapter](01-Getting_Started.md), we created and registered an extension feature on our adapter, but the extension had a major flaw - there were no operations on the feature for us to call! In this chapter, we will add a simple request-response method to our extension feature, and bind it so that it gets registered as a discoverable operation.

Our operation will allow a caller to specify a `PingMessage` object and receive a corresponding `PongMessage` object in return. Update the  `PingPongExtension` class as follows:

```csharp
public PongMessage Ping(IAdapterCallContext context, PingMessage message) {
    if (message == null) {
        throw new ArgumentNullException(nameof(message));
    }

    return new PongMessage() {
        CorrelationId = message.CorrelationId
    };
}
```

Note that we have specified a parameter of type `IAdapterCallContext`. This parameter is not strictly necessary, but it allows us to receive information about the calling user and authorize the call if our logic requires it.

Next, update the `PingPongExtension` constructor as follows:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    BindInvoke<PingPongExtension, PingMessage, PongMessage>(Ping);
}
```

The constructor now makes a call to the `BindInvoke` method to tell the base class that it should register an operation that can be called via the `Invoke` method on the `IAdapterExtensionFeature` interface. Here, we are calling the `BindInvoke<TFeature, TIn, TOut>` overload of the method, which allows us to tell the binding method that we expect the operation to receive an input parameter of type `PingMessage` and return a result of type `PongMessage`, and that the method should be added as an operation on the `PingPongExtension` extension registration.

Normally, the delegate signature for an invocable method is `Func<IAdapterCallContext, InvocationRequest, CancellationToken, Task<InvocationResponse>>`. However, the various `BindInvoke` methods inherited from the `AdapterExtensionFeature` base class allow registration of methods that have different signatures, converting to and from the specified input and output types.

Compile and run the program and we will see the following output:

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
        - Ping (asc:extensions/tutorial/ping-pong/invoke/Ping/)
          - Description:
```

Note that our `Ping` method is now listed as an invocable operation with its own URI that is derived from the extension URI. The `/invoke/` section towards the end of the URI indicates that the operation can be invoked via the `Invoke` method on the `IAdapterExtensionFeature` interface. However, we do not have a description. This is because we have not provided any operation metadata. We can do this by specifying additional optional parameters when calling the `BindInvoke` method. Replace the `BindInvoke` call as follows:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService, params IObjectEncoder[] encoders) : base(backgroundTaskService, encoders) {
    BindInvoke<PingPongExtension, PingMessage, PongMessage>(
        Ping,
        description: "Responds to a ping message with a pong message"
    );
}
```

We have now added some metadata that describes the operation. We could have also specified parameters describing the JSON schemas of our request and response types (`PingMessage` and `PongMessage` respectively). However, by not specifying these parameters, the `BindInvoke` will automatically generate these schemas for us.

Compile and run the program again and we will see the following output:

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
        - Ping (asc:extensions/tutorial/ping-pong/invoke/Ping/)
          - Description: Responds to a ping message with a pong message
```

The next step is for us to try invoking the operation.


### Invoking an Operation

When working with an in-process adapter, it is of course possible to retrieve the extension feature from the adapter's feature collection, and then cast it to the correct type and directly call a method on the extension feature type. However, this is not possible when trying to call an extension operation on an adapter that is running in an external process or on a remote server. This is where the methods defined on the `IAdapterExtensionFeature` interface come to the rescue.

In order to call our `Ping` method via a call to `Invoke`, we need pass in an `InvocationRequest` object that contains our `PingMessage`, and then process the resulting `InvocationResponse` object to extract the `PongMessage` result.

Fortunately, we can use extension methods for the `IAdapterExtensionFeature` type to perform the encoding and decoding for us.

Add the following code to the end of the `using` block in the `Run` method in `Runner.cs`:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
var correlationId = Guid.NewGuid().ToString();
var now = DateTime.UtcNow;
var pingMessage = new PingMessage() { CorrelationId = correlationId, UtcTime = now };
var pongMessage = await extensionFeature.Invoke<PingMessage, PongMessage>(
    context,
    new Uri("asc:extensions/tutorial/ping-pong/invoke/Ping/"),
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
        - Ping (asc:extensions/tutorial/ping-pong/invoke/Ping/)
          - Description: Responds to a ping message with a pong message

[INVOKE] Ping: 55be298d-17f5-49ca-9a42-fb14c53cfb85 @ 11:25:11 UTC
[INVOKE] Pong: 55be298d-17f5-49ca-9a42-fb14c53cfb85 @ 11:25:11 UTC
```


## Next Steps

In the [next chapter](03-Streaming_Methods.md), we will add a new method to our extension feature that shows how an extension can use server-to-client streaming to return results back to a caller.
