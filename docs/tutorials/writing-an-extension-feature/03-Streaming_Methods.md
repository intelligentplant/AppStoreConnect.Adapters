# Tutorial - Writing an Adapter Extension Feature

_This is part 3 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Streaming Methods

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-03)._

In the [previous chapter](02-Extension_Methods.md), we created a simple request-response operation for our extension feature. In this chapter, we will demonstrate another operation type: streaming methods. Streaming methods allow a caller to create a subscription on our feature and receive a stream of result objects back (using an `IAsyncEnumerable<T>` object). We keep on streaming results until either the feature decides to end the stream, or the caller cancels the subscription. This approach is also used in most standard adapter features.

First, we will add a method to our `PingPongExtension` class that will process streaming requests:

```csharp
public async IAsyncEnumerable<PongMessage> Ping(
    IAdapterCallContext context, 
    PingMessage message, 
    [EnumeratorCancellation]
    CancellationToken cancellationToken
) {
    if (message == null) {
        throw new ArgumentNullException(nameof(message));
    }

    while (!cancellationToken.IsCancellationRequested) {
        // Every second, we will return a new PongMessage
        await Task.Delay(1000, cancellationToken);

        yield return Ping(context, message);
    }
}
```

To register our new operation, we will call the `BindStream` method in our `PingPongExtension` constructor:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    // -- Existing BindInvoke call removed for brevity --
    
    BindStream<PingPongExtension, PingMessage, PongMessage>(
        Ping,
        description: "Responds to a ping message with a stream of pong messages"
    );
}
```

If you compile and run the program, you will notice that the streaming method is automatically added to the list of available operations:

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
      "name": "Ping Pong",
      "description": "Example extension feature.",
      "operations": {
        "asc:extensions/tutorial/ping-pong/stream/Ping/": {
          "operationType": "Stream",
          "name": "Ping",
          "description": "Responds to a ping message with a stream of pong messages",
          "requestSchema": {
            "type": "object",
            "properties": {
              "CorrelationId": {
                "type": "string",
                "description": "The correlation ID for the ping.",
                "required": []
              },
              "UtcTime": {
                "type": "string",
                "format": "date-time",
                "description": "The UTC time that the ping was sent at."
              }
            }
          },
          "responseSchema": {
            "type": "object",
            "properties": {
              "CorrelationId": {
                "type": "string",
                "description": "The correlation ID for the ping associated with this pong.",
                "required": []
              },
              "UtcTime": {
                "type": "string",
                "format": "date-time",
                "description": "The UTC time that the pong was sent at."
              }
            }
          }
        },
        "asc:extensions/tutorial/ping-pong/invoke/Ping/": {
          // Removed for brevity
        }
      }
    }
  }
}
```

Our next step is to subscribe to the stream. Replace the code to call the original operation in `Runner.cs` with the following:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
var correlationId = Guid.NewGuid().ToString();
var now = DateTime.UtcNow;
var pingMessage = new PingMessage() { CorrelationId = correlationId, UtcTime = now };

Console.WriteLine();
Console.WriteLine($"[STREAM] Ping: {correlationId} @ {now:HH:mm:ss} UTC");

try {
    await foreach (var pongMessage in extensionFeature.Stream<PingMessage, PongMessage>(
        context,
        new Uri("asc:extensions/tutorial/ping-pong/stream/Ping/"),
        pingMessage,
        cancellationToken
    )) {
        Console.WriteLine($"[STREAM] Pong: {pongMessage.CorrelationId} @ {pongMessage.UtcTime:HH:mm:ss} UTC");
    }
}
catch (OperationCanceledException) { }
```

When you compile and run the program again, you will see a pong message displayed every second until you cancel the subscription by pressing `CTRL+C` e.g.

```
-- Adapter summary removed for brevity --

[STREAM] Ping: ef84a7ec-b8eb-4fc1-9b45-720941b89ca1 @ 11:40:24 UTC
[STREAM] Pong: ef84a7ec-b8eb-4fc1-9b45-720941b89ca1 @ 11:40:25 UTC
[STREAM] Pong: ef84a7ec-b8eb-4fc1-9b45-720941b89ca1 @ 11:40:26 UTC
[STREAM] Pong: ef84a7ec-b8eb-4fc1-9b45-720941b89ca1 @ 11:40:27 UTC
[STREAM] Pong: ef84a7ec-b8eb-4fc1-9b45-720941b89ca1 @ 11:40:28 UTC
[STREAM] Pong: ef84a7ec-b8eb-4fc1-9b45-720941b89ca1 @ 11:40:29 UTC
```


## Next Steps

In the [next chapter](04-Duplex_Streaming_Methods.md), we will add a new method to our extension feature that shows how an extension can use bidirectional streaming to asynchronously receive and send requests and responses.
