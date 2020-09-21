# Tutorial - Writing an Adapter Extension Feature

_This is part 4 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Duplex Streaming Methods

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-04)._

In the [previous chapter](03-Streaming_Methods.md), we implemented a server-to-client streaming operation that allowed us to asynchronously push values to a caller until they cancelled their subscription. In this chapter, we will implement a bidirectional (or duplex) streaming operation, allowing a caller to stream values to the extension feature and also allowing the feature to stream values back to the caller. Just as a streaming response uses a `ChannelReader<T>` to return results to the caller, a `ChannelReader<T>` is also used to stream inputs to the operation.

Our first step is to add the duplex streaming method to our `PingPongExtension` class:

```csharp
[ExtensionFeatureOperation(
    Description = "Responds to each ping message in an incoming stream with a pong message",
    InputParameterDescription = "The ping message",
    OutputParameterDescription = "The pong message"
)]
public Task<ChannelReader<PongMessage>> Ping(ChannelReader<PingMessage> messages, CancellationToken cancellationToken) {
    if (messages == null) {
        throw new ArgumentNullException(nameof(messages));
    }

    var result = Channel.CreateUnbounded<PongMessage>();
    result.Writer.RunBackgroundOperation(async (ch, ct) => {
        while (await messages.WaitToReadAsync(ct).ConfigureAwait(false)) {
            while (messages.TryRead(out var message)) {
                if (message == null) {
                    continue;
                }

                ch.TryWrite(new PongMessage() {
                    CorrelationId = message.CorrelationId
                });
            }
        }
    }, true, BackgroundTaskService, cancellationToken);

    return Task.FromResult(result.Reader);
}
```

In our background operation above, we simply wait for ping messages to be written to the incoming stream, and then write a corresponding pong message to the output stream. Next, we use the `BindDuplexStream` method in our constructor to register the new method with our extension:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    BindInvoke<PingMessage, PongMessage>(Ping);
    BindStream<PingMessage, PongMessage>(Ping);
    BindDuplexStream<PingMessage, PongMessage>(Ping);
}
```

Compiling and running the program at this point will show the new operation in our adapter summary:

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
```

To test the new method, we will create a `Channel<PingMessage>` that we will write to at a random interval, and then read the corresponding pong messages from our subscription channel. Replace the code for calling the streaming operation in `Program.cs` with the following:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
var pingMessageStream = Channel.CreateUnbounded<PingMessage>();

var pongMessageStream = await extensionFeature.DuplexStream<PingMessage, PongMessage>(
    context,
    new Uri("asc:extensions/tutorial/ping-pong/Ping/DuplexStream/"),
    pingMessageStream,
    cancellationToken
);

Console.WriteLine();

pingMessageStream.Writer.RunBackgroundOperation(async (ch, ct) => {
    var rnd = new Random();
    while (!ct.IsCancellationRequested) {
        // Delay for up to 5 seconds.
        var delay = TimeSpan.FromMilliseconds(5000 * rnd.NextDouble());
        if (delay > TimeSpan.Zero) {
            await Task.Delay(delay, ct);
        }
        var pingMessage = new PingMessage() { CorrelationId = Guid.NewGuid().ToString() };
        Console.WriteLine($"[DUPLEX STREAM] Ping: {pingMessage.CorrelationId} @ {pingMessage.UtcTime:HH:mm:ss} UTC");
        await ch.WriteAsync(pingMessage, ct);
    }
}, true, cancellationToken: cancellationToken);

await foreach (var pongMessage in pongMessageStream.ReadAllAsync(cancellationToken)) {
    Console.WriteLine($"[DUPLEX STREAM] Pong: {pongMessage.CorrelationId} @ {pongMessage.UtcTime:HH:mm:ss} UTC");
}
```

Compile and run the program. You will see output similar to the following:

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
        - Ping (asc:extensions/tutorial/ping-pong/Ping/DuplexStream/)
          - Description: Responds to each ping message in an incoming stream with a pong message
        - Ping (asc:extensions/tutorial/ping-pong/Ping/Stream/)
          - Description: Responds to a ping message with a pong message every second until the call is cancelled

[DUPLEX STREAM] Ping: 28290dfa-7616-4d03-8859-856928467dcf @ 09:38:36 UTC
[DUPLEX STREAM] Pong: 28290dfa-7616-4d03-8859-856928467dcf @ 09:38:36 UTC
[DUPLEX STREAM] Ping: f80ef849-d587-4384-bf14-da2375685b5c @ 09:38:39 UTC
[DUPLEX STREAM] Pong: f80ef849-d587-4384-bf14-da2375685b5c @ 09:38:39 UTC
[DUPLEX STREAM] Ping: f34d7845-5318-4c01-80e6-f148e7823f66 @ 09:38:39 UTC
[DUPLEX STREAM] Pong: f34d7845-5318-4c01-80e6-f148e7823f66 @ 09:38:39 UTC
[DUPLEX STREAM] Ping: 1331cd01-c5a8-4d55-98d3-fb3a9a18d769 @ 09:38:40 UTC
[DUPLEX STREAM] Pong: 1331cd01-c5a8-4d55-98d3-fb3a9a18d769 @ 09:38:40 UTC
[DUPLEX STREAM] Ping: b893e37d-7a21-49c5-b60b-af72e69133ca @ 09:38:43 UTC
[DUPLEX STREAM] Pong: b893e37d-7a21-49c5-b60b-af72e69133ca @ 09:38:43 UTC
```


## Next Steps

We've now implemented all of the available operation types on our extension feature. In the [next chapter](05-Implementing_Multiple_Extensions.md), we will learn how to define multiple discrete extension features on the same implementation class.
