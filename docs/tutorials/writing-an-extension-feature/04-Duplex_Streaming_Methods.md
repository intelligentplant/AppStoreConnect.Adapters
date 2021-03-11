# Tutorial - Writing an Adapter Extension Feature

_This is part 4 of a tutorial series about creating an extension feature for an adapter. The introduction to the series can be found [here](00-Introduction.md)._


## Duplex Streaming Methods

_The full code for this chapter can be found [here](/examples/tutorials/writing-an-extension-feature/chapter-04)._

In the [previous chapter](03-Streaming_Methods.md), we implemented a server-to-client streaming operation that allowed us to asynchronously push values to a caller until they cancelled their subscription. In this chapter, we will implement a bidirectional (or duplex) streaming operation, allowing a caller to stream values to the extension feature and also allowing the feature to stream values back to the caller. Just as a streaming response uses a `ChannelReader<T>` to return results to the caller, a `ChannelReader<T>` is also used to stream inputs to the operation.

To register our new operation, we will call the `BindDuplexStream` method in our `PingPongExtension` constructor:

```csharp
public PingPongExtension(IBackgroundTaskService backgroundTaskService) : base(backgroundTaskService) {
    // -- Existing BindInvoke and BindStream calls removed for brevity --

    BindDuplexStream<PingPongExtension>(
        // Handler
        (ctx, req, inChannel, ct) => {
            // The handler delegate requires that we return a Task<ChannelReader<InvocationResponse>>.
            var outChannel = Channel.CreateUnbounded<InvocationResponse>();

            // Start a background task that will write results into our channel whenever 
            // we receive a new input.
            outChannel.Writer.RunBackgroundOperation(async (ch, ct2) => {
                // First, we process the ping message in the original request.
                var pingMessage = Decode<PingMessage>(req.Arguments.FirstOrDefault());
                var pongMessage = Ping(pingMessage);
                await ch.WriteAsync(new InvocationResponse() {
                    Results = new[] { Encode(pongMessage) }
                }, ct2);

                // Now, we process the additional ping messages that are streamed into the 
                // inChannel.
                await foreach (var update in inChannel.ReadAllAsync(ct2)) {
                    pingMessage = Decode<PingMessage>(update.Arguments.FirstOrDefault());
                    pongMessage = Ping(pingMessage);
                    await ch.WriteAsync(new InvocationResponse() {
                        Results = new[] { Encode(pongMessage) }
                    }, ct2);
                }
            }, true, backgroundTaskService, ct);

            // Return the reader portion of the channel.
            return Task.FromResult(outChannel.Reader);
        },
        // Operation name
        nameof(Ping),
        // Description
        "Responds to each ping message in the incoming stream with a pong message",
        // Input parameter descriptions
        new[] {
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
        - Ping (asc:extensions/tutorial/ping-pong/stream/Ping/)
          - Description: Responds to a ping message with a stream of pong messages
        - Ping (asc:extensions/tutorial/ping-pong/invoke/Ping/)
          - Description: Responds to a ping message with a pong message
        - Ping (asc:extensions/tutorial/ping-pong/duplexstream/Ping/)
          - Description: Responds to each ping message in the incoming stream with a pong message
```

To test the new method, we will create a `Channel<InvocationStreamItem>` that we will write a ping message to at a random interval, and then read the corresponding pong messages from our subscription channel. Replace the code for calling the streaming operation in `Runner.cs` with the following:

```csharp
var extensionFeature = adapter.GetFeature<IAdapterExtensionFeature>("asc:extensions/tutorial/ping-pong/");
var correlationId = Guid.NewGuid().ToString();
var now = DateTime.UtcNow;
var pingMessage = new PingMessage() { CorrelationId = correlationId, UtcTime = now };
var pingMessageStream = Channel.CreateUnbounded<InvocationStreamItem>();

var channel = await extensionFeature.DuplexStream(
    context,
    new InvocationRequest() {
        OperationId = new Uri("asc:extensions/tutorial/ping-pong/duplexstream/Ping/"),
        Arguments = new[] { EncodedObject.Create(pingMessage, DataCore.Adapter.Json.JsonObjectEncoder.Default) }
    },
    pingMessageStream,
    cancellationToken
);

Console.WriteLine();
Console.WriteLine($"[DUPLEX STREAM] Ping: {pingMessage.CorrelationId} @ {pingMessage.UtcTime:HH:mm:ss} UTC");

pingMessageStream.Writer.RunBackgroundOperation(async (ch, ct) => {
    var rnd = new Random();
    while (!ct.IsCancellationRequested) {
        // Delay for up to 2 seconds.
        var delay = TimeSpan.FromMilliseconds(2000 * rnd.NextDouble());
        if (delay > TimeSpan.Zero) {
            await Task.Delay(delay, ct);
        }
        var pingMessage = new PingMessage() { CorrelationId = Guid.NewGuid().ToString() };

        Console.WriteLine($"[DUPLEX STREAM] Ping: {pingMessage.CorrelationId} @ {pingMessage.UtcTime:HH:mm:ss} UTC");
        await ch.WriteAsync(new InvocationStreamItem() { 
            Arguments = new[] { EncodedObject.Create(pingMessage, DataCore.Adapter.Json.JsonObjectEncoder.Default) }
        }, ct);
    }
}, true, cancellationToken: cancellationToken);

await foreach (var response in channel.ReadAllAsync(cancellationToken)) {
    var pongMessage = DataCore.Adapter.Json.JsonObjectEncoder.Default.Decode<PongMessage>(response.Results.FirstOrDefault());
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
        - Ping (asc:extensions/tutorial/ping-pong/stream/Ping/)
          - Description: Responds to a ping message with a stream of pong messages
        - Ping (asc:extensions/tutorial/ping-pong/invoke/Ping/)
          - Description: Responds to a ping message with a pong message
        - Ping (asc:extensions/tutorial/ping-pong/duplexstream/Ping/)
          - Description: Responds to each ping message in the incoming stream with a pong message

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
