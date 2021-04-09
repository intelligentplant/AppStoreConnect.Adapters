using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Events;

using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {

    /// <summary>
    /// Client for querying adapter alarm and event messages.
    /// </summary>
    public class EventsClient {

        /// <summary>
        /// The adapter SignalR client that manages the connection.
        /// </summary>
        private readonly AdapterSignalRClient _client;


        /// <summary>
        /// Creates a new <see cref="EventsClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter SignalR client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public EventsClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Creates a channel to receive event messages in real-time from the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   Additional subscription properties.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation. If this token fires, or the connection is
        ///   lost, the channel will be closed.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a channel that is used to stream the 
        ///   event messages back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async IAsyncEnumerable<EventMessage> CreateEventMessageChannelAsync(
            string adapterId, 
            CreateEventMessageSubscriptionRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<EventMessage>(
                "CreateEventMessageChannel",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Creates a topic-based event subscription.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   Additional subscription properties.
        /// </param>
        /// <param name="channel">
        ///   A channel that can be used to add topics to or remove topics from the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the channel reader for the subscription.
        /// </returns>
        public async IAsyncEnumerable<EventMessage> CreateEventMessageTopicChannelAsync(
            string adapterId,
            CreateEventMessageTopicSubscriptionRequest request,
            IAsyncEnumerable<EventMessageSubscriptionUpdate> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);

            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                await foreach (var item in connection.StreamAsync<EventMessage>(
                    "CreateEventMessageTopicChannel",
                    adapterId,
                    request,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false)) {
                    yield return item;
                }
                yield break;
            }

            // We are using ASP.NET Core 2.x, so we cannot use client-to-server streaming. Instead, 
            // we will make a separate streaming call for each topic, and cancel it when we detect 
            // that the topic has been unsubscribed from.

            // This is our single output channel.
            var result = Channel.CreateUnbounded<EventMessage>(new UnboundedChannelOptions() { 
                SingleWriter = false
            });

            // Cancellation token source for each subscribed topic, indexed by topic name.
            var subscriptions = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);

            // Task completion source that we will complete when the cancellation token passed to 
            // the method fires.
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ctReg = cancellationToken.Register(() => {
                result.Writer.TryComplete();
                tcs.TrySetResult(true);
            });

            // Local function that will run the subscription for a topic until the cancellation 
            // token fires.
            async Task RunTopicSubscription(string topic, CancellationToken ct) {
                var req = new CreateEventMessageTopicSubscriptionRequest() { 
                    SubscriptionType = request.SubscriptionType,
                    Properties = request.Properties,
                    Topics = new [] { topic }
                };
                await foreach (var item in connection.StreamAsync<EventMessage>(
                    "CreateEventMessageTopicChannel",
                    adapterId,
                    req,
                    ct
                ).ConfigureAwait(false)) {
                    await result!.Writer.WriteAsync(item, ct).ConfigureAwait(false);
                }
            }

            // Local function that will add or remove subscriptions for individual topics.
            void ProcessTopicSubscriptionChange(IEnumerable<string> topics, bool added) {
                foreach (var topic in topics) {
                    if (topic == null) {
                        continue;
                    }

                    if (added) {
                        if (subscriptions!.ContainsKey(topic)) {
                            continue;
                        }

                        var ctSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        var ct = ctSource.Token;
                        subscriptions[topic] = ctSource;
                        _ = Task.Run(async () => {
                            try {
                                await RunTopicSubscription(topic, ct).ConfigureAwait(false);
                            }
                            catch { }
                            finally {
                                if (subscriptions!.TryRemove(topic, out var cts)) {
                                    cts.Cancel();
                                    cts.Dispose();
                                }
                            }
                        }, ct);
                    }
                    else {
                        if (subscriptions!.TryRemove(topic, out var ctSource)) {
                            ctSource.Cancel();
                            ctSource.Dispose();
                        }
                    }
                }
            }

            // Background task that will add/remove subscriptions to indivudual topics subscription 
            // changes occur.
            _ = Task.Run(async () => { 
                try {
                    if (request.Topics.Any()) {
                        ProcessTopicSubscriptionChange(request.Topics, true);
                    }

                    await foreach (var update in channel.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                        if (update?.Topics == null || !update.Topics.Any()) {
                            continue;
                        }

                        ProcessTopicSubscriptionChange(update.Topics, update.Action == SubscriptionUpdateAction.Subscribe);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (Exception e) {
                    result.Writer.TryComplete(e);
                }
                finally {
                    // Ensure that we wait until the cancellation token for the overall subscription has 
                    // actually fired before we dispose of any remaining subscriptions.
                    await tcs.Task.ConfigureAwait(false);
                    ctReg.Dispose();
                    result.Writer.TryComplete();
                    foreach (var topic in subscriptions.Keys.ToArray()) {
                        if (subscriptions!.TryRemove(topic, out var ctSource)) {
                            ctSource.Cancel();
                            ctSource.Dispose();
                        }
                    }
                }
            }, cancellationToken);

            while (await result.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (result.Reader.TryRead(out var item)) {
                    yield return item;
                }
            }
        }


        /// <summary>
        /// Reads historical event messages from an adapter using a time range.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the event messages back to 
        ///   the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async IAsyncEnumerable<EventMessage> ReadEventMessagesAsync(
            string adapterId, 
            ReadEventMessagesForTimeRangeRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<EventMessage>(
                "ReadEventMessagesForTimeRange",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Reads historical event messages from an adapter using an initial cursor position.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the event messages back to 
        ///   the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
        ///   <paramref name="request"/> fails validation.
        /// </exception>
        public async IAsyncEnumerable<EventMessageWithCursorPosition> ReadEventMessagesAsync(
            string adapterId, 
            ReadEventMessagesUsingCursorRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<EventMessageWithCursorPosition>(
                "ReadEventMessagesUsingCursor",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Writes event messages to an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="channel">
        ///   The channel that will emit the items to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the write result for each 
        ///   event message back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async Task<ChannelReader<WriteEventMessageResult>> WriteEventMessagesAsync(string adapterId, ChannelReader<WriteEventMessageItem> channel, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                return await connection.StreamAsChannelAsync<WriteEventMessageResult>(
                    "WriteEventMessages",
                    adapterId,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false);
            }

            // We are using ASP.NET Core 2.x, so we cannot use bidirectional streaming. Instead, 
            // we will read the channel ourselves and make an invocation call for every value.
            var result = Channel.CreateUnbounded<WriteEventMessageResult>();

            _ = Task.Run(async () => { 
                try {
                    while (await channel.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                        while (channel.TryRead(out var val)) {
                            if (cancellationToken.IsCancellationRequested) {
                                break;
                            }
                            if (val == null) {
                                continue;
                            }

                            var writeResult = await connection.InvokeAsync<WriteEventMessageResult>(
                                "WriteEventMessage",
                                adapterId,
                                val,
                                cancellationToken
                            ).ConfigureAwait(false);

                            await result.Writer.WriteAsync(writeResult, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e) {
#pragma warning restore CA1031 // Do not catch general exception types
                    result.Writer.TryComplete(e);
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, cancellationToken);

            return result.Reader;
        }

    }
}
