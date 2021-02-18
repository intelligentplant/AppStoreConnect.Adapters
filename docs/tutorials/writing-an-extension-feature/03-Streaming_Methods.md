# Tutorial - Writing an Adapter Extension Feature

_This is part 3 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Streaming Methods

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-03)._

In the [previous chapter](02-Extension_Methods.md), we created a simple request-response operation for our extension feature. In this chapter, we will demonstrate another operation type: streaming methods. Streaming methods allow a caller to create a subscription on our feature and receive a stream of result objects back (using a `ChannelReader<T>` object). We keep on streaming results until either the feature decides to end the stream, or the caller cancels the subscription. This approach is also used in most standard adapter features.

To start, we will create the streaming method on our `PingPongExtension` class:

```csharp
[ExtensionFeatureOperation(
    Description = "Responds to a ping message with a pong message every second until the call is cancelled",
    InputParameterDescription = "The ping message",
    OutputParameterDescription = "The pong message"
)]
public ChannelReader<PongMessage> Ping(PingMessage message, CancellationToken cancellationToken) {
    if (message == null) {
        throw new ArgumentNullException(nameof(message));
    }

    var result = Channel.CreateUnbounded<PongMessage>();
    result.Writer.RunBackgroundOperation(async (ch, ct) => { 
        while (!ct.IsCancellationRequested) {
            await Task.Delay(1000, ct).ConfigureAwait(false);
            ch.TryWrite(new PongMessage() {
                CorrelationId = message.CorrelationId
            });
        }
    }, true, BackgroundTaskService, cancellationToken);

    return result.Reader;
}
```

Note the use of the `RunBackgroundOperation` extension method on the writer for the result channel. This allows us to kick off a background task that will run until the caller cancels their subscription, and ensures that the writer for the result channel is completed when the background task is cancelled.

To register this new `Ping` overload, we call the `BindStream` method in our `PingPongExtension` constructor:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    BindInvoke<PingMessage, PongMessage>(Ping);
    BindStream<PingMessage, PongMessage>(Ping);
}
```

If you compile and run the program, you will notice that the streaming method is automatically added to the list of available operations:

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
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Invoke/)
          - Description: Responds to a ping message with a pong message
```

Our next step is to subscribe to the stream. Replace the code to call the original operation in `Runner.cs` with the following:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
var correlationId = Guid.NewGuid().ToString();
var pingMessage = new PingMessage() { CorrelationId = correlationId };
var pongMessageStream = await extensionFeature.Stream<PingMessage, PongMessage>(
    context,
    new Uri("asc:extensions/tutorial/ping-pong/Ping/Stream/"),
    pingMessage,
    cancellationToken
);

Console.WriteLine();
Console.WriteLine($"[STREAM] Ping: {pingMessage.CorrelationId} @ {pingMessage.UtcTime:HH:mm:ss} UTC");
await foreach (var pongMessage in pongMessageStream.ReadAllAsync(cancellationToken)) {
    Console.WriteLine($"[STREAM] Pong: {pongMessage.CorrelationId} @ {pongMessage.UtcTime:HH:mm:ss} UTC");
}
```

When you compile and run the program again, you will see a pong message displayed every second until you cancel the subscription by pressing `CTRL+C` e.g.

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
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Invoke/)
          - Description: Responds to a ping message with a pong message

[STREAM] Ping: 14810b0f-7045-401e-8f50-581d6252eafa @ 08:48:28 UTC
[STREAM] Pong: 14810b0f-7045-401e-8f50-581d6252eafa @ 08:48:29 UTC
[STREAM] Pong: 14810b0f-7045-401e-8f50-581d6252eafa @ 08:48:30 UTC
[STREAM] Pong: 14810b0f-7045-401e-8f50-581d6252eafa @ 08:48:31 UTC
[STREAM] Pong: 14810b0f-7045-401e-8f50-581d6252eafa @ 08:48:32 UTC
```


## Next Steps

In the [next chapter](04-Duplex_Streaming_Methods.md), we will add a new method to our extension feature that shows how an extension can use bidirectional streaming to asynchronously receive and send requests and responses.
