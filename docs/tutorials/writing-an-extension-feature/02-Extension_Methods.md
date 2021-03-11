# Tutorial - Writing an Adapter Extension Feature

_This is part 2 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Extension Methods

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-02)._

In the [previous chapter](01-Getting_Started.md), we created and registered an extension feature on our adapter, but the extension had a major flaw - there were no operations on the feature for us to call! In this chapter, we will add a simple request-response method to our extension feature, and bind it so that it gets registered as a discoverable operation.

Our operation will allow a caller to specify a `PingMessage` object and receive a corresponding `PongMessage` object in return. Update the  `PingPongExtension` as follows:

```csharp
public PongMessage Ping(PingMessage message) {
    if (message == null) {
        throw new ArgumentNullException(nameof(message));
    }

    return new PongMessage() {
        CorrelationId = message.CorrelationId
    };
}
```

Next, update the `PingPongExtension` constructor as follows:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService, params IObjectEncoder[] encoders) : base(backgroundTaskService, encoders) {
    BindInvoke<PingPongExtension>(
        // Handler
        (ctx, req, ct) => {
            var pingMessage = Decode<PingMessage>(req.Arguments.FirstOrDefault());
            var pongMessage = Ping(pingMessage);
            return Task.FromResult(new InvocationResponse() {
                Results = new[] { Encode(pongMessage) }
            });
        }, 
        // Operation name
        nameof(Ping), 
        // Description
        "Responds to a ping message with a pong message", 
        // Input parameter descriptions
        new [] {
            new ExtensionFeatureOperationParameterDescriptor() {
                Ordinal = 0,
                TypeId = TypeLibrary.GetTypeId<PingMessage>(),
                Description = "The ping message"
            }
        },
        // Output parameter descriptions
        new[] {
            new ExtensionFeatureOperationParameterDescriptor() {
                Ordinal = 1,
                TypeId = TypeLibrary.GetTypeId<PongMessage>(),
                Description = "The pong message"
            }
        }
    );
}
```

The constructor now makes a call to the `BindInvoke<TFeature>` method to tell the base class that it should register an operation that can be called via the `Invoke` method on the `IAdapterExtensionFeature` interface. We provide a handler delegate that matches the required signature, as well as name for the operation and additional metadata such as parameter descriptions.

Let's take a closer look at the handler:

```csharp
(ctx, req, ct) => {
    var pingMessage = Decode<PingMessage>(req.Arguments.FirstOrDefault());
    return Task.FromResult(new InvocationResponse() {
        Results = new[] { Encode(Ping(pingMessage)) }
    }
}     
```

The handler for an `Invoke` operation accepts three parameters: an `IAdapterCallContext` that can be used to identify the caller, an [InvocationRequest](/src/DataCore.Adapter.Core/Extensions/InvocationRequest.cs) that contains the input parameters for the operation, and a `CancellationToken` supplied by the caller. The handler returns a task that returns an [InvocationResponse](/src/DataCore.Adapter.Core/Extensions/InvocationResponse.cs) containing the results of the operation.

Parameters and results in the `InvocationRequest` and `InvocationResponse` types are specified as instances of the [EncodedObject](/src/DataCore.Adapter.Core/Common/EncodedObject.cs).  We can use the `Decode<T>` and `Encode<T>` methods inherited by our `PingPongExtension` class to decode incoming `PingMessage` objects and decode outgoing `PongMessage` objects.

Compile and run the program again and we will see output similar to the following:

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

Note that our `Ping` method is now listed as an invocable operation with its own URI that is derived from the extension URI. The `/invoke/` section towards the end of the URI indicates that the operation can be invoked via the `Invoke` method on the `IAdapterExtensionFeature` interface. 

The next step is for us to try invoking the operation.


### Invoking an Operation

When working with an in-process adapter, it is of course possible to retrieve the extension feature from the adapter's feature collection, and then cast it to the correct type and directly call a method on the extension feature type. However, this is not possible when trying to call an extension operation on an adapter that is running in an external process or on a remote server. This is where the methods defined on the `IAdapterExtensionFeature` interface come to the rescue.

In order to call our `Ping` method via a call to `Invoke`, we need pass in an `InvocationRequest` object that contains our encoded `PingMessage`, and then process the resulting `InvocationResponse` object to extract the `PongMessage` result.

Add the following code to the end of the `using` block in the `Run` method in `Runner.cs`:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
var correlationId = Guid.NewGuid().ToString();
var now = DateTime.UtcNow;
var pingMessage = new PingMessage() { CorrelationId = correlationId, UtcTime = now };
var response = await extensionFeature.Invoke(
    context,
    new InvocationRequest() { 
        OperationId = new Uri("asc:extensions/tutorial/ping-pong/invoke/Ping/"),
        Arguments = new [] { EncodedObject.Create(pingMessage, DataCore.Adapter.Json.JsonObjectEncoder.Default) }
    },
    cancellationToken
);
var pongMessage = DataCore.Adapter.Json.JsonObjectEncoder.Default.Decode<PongMessage>(response.Results.FirstOrDefault());

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
