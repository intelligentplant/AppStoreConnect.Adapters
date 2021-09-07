# Tutorial - Writing an Adapter Extension Feature

_This is part 4 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Duplex Streaming Methods

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-04)._

In the [previous chapter](03-Streaming_Methods.md), we implemented a server-to-client streaming operation that allowed us to asynchronously push values to a caller until they cancelled their subscription. In this chapter, we will implement a bidirectional (or duplex) streaming operation, allowing a caller to stream values to the extension feature and also allowing the feature to stream values back to the caller. Just as a streaming response uses an `IAsyncEnumerable<T>` to return results to the caller, an `IAsyncEnumerable<T>` is also used to stream inputs to the operation.

First, we will add a method to our `PingPongExtension` class that will process duplex streaming requests:

```csharp
        public async IAsyncEnumerable<PongMessage> Ping(
            IAdapterCallContext context, 
            IAsyncEnumerable<PingMessage> messages, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (messages == null) {
                throw new ArgumentNullException(nameof(messages));
            }

            await foreach (var pingMessage in messages.WithCancellation(cancellationToken)) {
                yield return Ping(context, pingMessage);
            }
        }
```

To register our new operation, we will call the `BindDuplexStream` method in our `PingPongExtension` constructor:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    // -- Existing BindInvoke and BindStream calls removed for brevity --

    BindDuplexStream<PingPongExtension, PingMessage, PongMessage>(
        Ping,
        description: "Responds to each ping message in the incoming stream with a pong message"
    );
}
```

Compiling and running the program at this point will show the new operation in our adapter summary:

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
        "asc:extensions/tutorial/ping-pong/duplexstream/Ping/": {
          "operationType": "DuplexStream",
          "name": "Ping",
          "description": "Responds to each ping message in the incoming stream with a pong message",
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
        },
        "asc:extensions/tutorial/ping-pong/stream/Ping/": {
          // Removed for brevity
        }
      }
    }
  }
}
```

To test the new method, we will create a `Channel<PingMessage>` that we will write a ping message to at a random interval in a background task, and then read the corresponding pong messages from our subscription. Replace the code for calling the streaming operation in `Runner.cs` with the following:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
var correlationId = Guid.NewGuid().ToString();
var now = DateTime.UtcNow;
var pingMessage = new PingMessage() { CorrelationId = correlationId, UtcTime = now };
var pingMessageStream = Channel.CreateUnbounded<PingMessage>();

Console.WriteLine();

_ = Task.Run(async () => { 
    try {
        var rnd = new Random();
        while (!cancellationToken.IsCancellationRequested) {
            // Delay for up to 2 seconds.
            var delay = TimeSpan.FromMilliseconds(2000 * rnd.NextDouble());
            if (delay > TimeSpan.Zero) {
                await Task.Delay(delay, cancellationToken);
            }
            var pingMessage = new PingMessage() { CorrelationId = Guid.NewGuid().ToString() };

            Console.WriteLine($"[DUPLEX STREAM] Ping: {pingMessage.CorrelationId} @ {pingMessage.UtcTime:HH:mm:ss} UTC");
            await pingMessageStream.Writer.WriteAsync(pingMessage, cancellationToken);
        }
    }
    catch { }
    finally {
        pingMessageStream.Writer.TryComplete();
    }
}, cancellationToken);

await foreach (var pongMessage in extensionFeature.DuplexStream<PingMessage, PongMessage>(
    context,
    new Uri("asc:extensions/tutorial/ping-pong/duplexstream/Ping/"),
    pingMessageStream.Reader.ReadAllAsync(cancellationToken),
    null,
    cancellationToken
)) {
    Console.WriteLine($"[DUPLEX STREAM] Pong: {pongMessage.CorrelationId} @ {pongMessage.UtcTime:HH:mm:ss} UTC");
}
```

Compile and run the program. You will see output similar to the following:

```
-- Adapter summary removed for brevity --

[DUPLEX STREAM] Ping: 76640863-e5a5-474a-9593-671ba698a90f @ 12:27:35 UTC
[DUPLEX STREAM] Pong: 76640863-e5a5-474a-9593-671ba698a90f @ 12:27:35 UTC
[DUPLEX STREAM] Ping: 3e5ac4b7-6fab-4c5a-9c85-9a7dcd0a4cf4 @ 12:27:36 UTC
[DUPLEX STREAM] Pong: 3e5ac4b7-6fab-4c5a-9c85-9a7dcd0a4cf4 @ 12:27:36 UTC
[DUPLEX STREAM] Ping: c8f1d50d-2d3c-4d5c-a554-a08b53622a7d @ 12:27:37 UTC
[DUPLEX STREAM] Pong: c8f1d50d-2d3c-4d5c-a554-a08b53622a7d @ 12:27:37 UTC
[DUPLEX STREAM] Ping: 899d9e86-168a-494f-9a51-9b43cd57d12b @ 12:27:38 UTC
[DUPLEX STREAM] Pong: 899d9e86-168a-494f-9a51-9b43cd57d12b @ 12:27:38 UTC
[DUPLEX STREAM] Ping: 623147a2-5537-4e71-a00f-6e9f370a1026 @ 12:27:39 UTC
[DUPLEX STREAM] Pong: 623147a2-5537-4e71-a00f-6e9f370a1026 @ 12:27:39 UTC
[DUPLEX STREAM] Ping: 806efd9c-cd45-4648-94b6-03f17aeb8ae5 @ 12:27:40 UTC
[DUPLEX STREAM] Pong: 806efd9c-cd45-4648-94b6-03f17aeb8ae5 @ 12:27:40 UTC
```


## Next Steps

We've now implemented all of the available operation types on our extension feature. In the [next chapter](05-Implementing_Multiple_Extensions.md), we will learn how to define multiple discrete extension features on the same implementation class.
