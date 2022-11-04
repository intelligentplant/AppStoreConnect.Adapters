using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {

    /// <summary>
    /// Client for querying adapter tag values.
    /// </summary>
    public class TagValuesClient {

        /// <summary>
        /// The adapter SignalR client that manages the connection.
        /// </summary>
        private readonly AdapterSignalRClient _client;


        /// <summary>
        /// Creates a new <see cref="TagValuesClient"/> object.
        /// </summary>
        /// <param name="client">
        ///   The adapter SignalR client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public TagValuesClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        /// <summary>
        /// Creates a snapshot tag value subscription.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID to subscribe to.
        /// </param>
        /// <param name="request">
        ///   The subscription request.
        /// </param>
        /// <param name="channel">
        ///   A channel that can be used to add tags to or remove tags from the subscription.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A channel reader that wil emit values for the subscription.
        /// </returns>
        public async IAsyncEnumerable<TagValueQueryResult> CreateSnapshotTagValueChannelAsync(
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest request,
            IAsyncEnumerable<TagValueSubscriptionUpdate> channel,
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

#pragma warning disable CS0618 // Type or member is obsolete
            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                await foreach (var item in connection.StreamAsync<TagValueQueryResult>(
                    "CreateSnapshotTagValueChannel",
                    adapterId,
                    request,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false)) {
                    yield return item;
                }
                yield break;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            // We are using ASP.NET Core 2.x, so we cannot use client-to-server streaming. Instead, 
            // we will make a separate streaming call for each topic, and cancel it when we detect 
            // that the topic has been unsubscribed from.

            // This is our single output channel.
            var result = Channel.CreateUnbounded<TagValueQueryResult>(new UnboundedChannelOptions() {
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
                var req = new CreateSnapshotTagValueSubscriptionRequest() {
                    Properties = request.Properties,
                    Tags = new[] { topic }
                };
                await foreach (var item in connection.StreamAsync<TagValueQueryResult>(
                    "CreateSnapshotTagValueChannel",
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
                    if (request.Tags.Any()) {
                        ProcessTopicSubscriptionChange(request.Tags, true);
                    }

                    await foreach (var update in channel.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                        if (update?.Tags == null || !update.Tags.Any()) {
                            continue;
                        }

                        ProcessTopicSubscriptionChange(update.Tags, update.Action == SubscriptionUpdateAction.Subscribe);
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
        /// Polls an adapter for snapshot tag values.
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the results.
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValuesAsync(
            string adapterId, 
            ReadSnapshotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<TagValueQueryResult>(
                "ReadSnapshotTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Polls an adapter for raw tag values.
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the results.
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadRawTagValuesAsync(
            string adapterId, 
            ReadRawTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<TagValueQueryResult>(
                "ReadRawTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Polls an adapter for visualisation-friendly tag values.
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the results.
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadPlotTagValuesAsync(
            string adapterId, 
            ReadPlotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<TagValueQueryResult>(
                "ReadPlotTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Polls an adapter for tag values at specific timestamps.
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the results.
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
        public async IAsyncEnumerable<TagValueQueryResult> ReadTagValuesAtTimesAsync(
            string adapterId, 
            ReadTagValuesAtTimesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<TagValueQueryResult>(
                "ReadTagValuesAtTimes",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Gets the data functions that an adapter supports when calling 
        /// <see cref="ReadProcessedTagValuesAsync(string, ReadProcessedTagValuesRequest, CancellationToken)"/>.
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the results.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async IAsyncEnumerable<DataFunctionDescriptor> GetSupportedDataFunctionsAsync(
            string adapterId, 
            GetSupportedDataFunctionsRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<DataFunctionDescriptor>(
                "GetSupportedDataFunctionsWithRequest",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Gets the data functions that an adapter supports when calling 
        /// <see cref="ReadProcessedTagValuesAsync(string, ReadProcessedTagValuesRequest, CancellationToken)"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the results.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        [Obsolete("Use GetSupportedDataFunctionsAsync(string, GetSupportedDataFunctionsRequest, CancellationToken) instead.", false)]
        public async IAsyncEnumerable<DataFunctionDescriptor> GetSupportedDataFunctionsAsync(
            string adapterId,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<DataFunctionDescriptor>(
                "GetSupportedDataFunctions",
                adapterId,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Polls an adapter for processed/aggregated tag values.
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
        ///   An <see cref="IAsyncEnumerable{T}"/> that will return the results.
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
        public async IAsyncEnumerable<ProcessedTagValueQueryResult> ReadProcessedTagValuesAsync(
            string adapterId, 
            ReadProcessedTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            AdapterSignalRClient.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            await foreach (var item in connection.StreamAsync<ProcessedTagValueQueryResult>(
                "ReadProcessedTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Writes a stream of tag values to an adapter's snapshot.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="channel">
        ///   The channel that will emit the items to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the write result for each 
        ///   tag value back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async IAsyncEnumerable<WriteTagValueResult> WriteSnapshotTagValuesAsync(
            string adapterId,
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel,
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
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
#pragma warning disable CS0618 // Type or member is obsolete
            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                await foreach (var item in connection.StreamAsync<WriteTagValueResult>(
                    "WriteSnapshotTagValues",
                    adapterId,
                    request,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false)) {
                    yield return item;
                }
                yield break;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            // We are using ASP.NET Core 2.x, so we cannot use bidirectional streaming. Instead, 
            // we will read the channel ourselves and make an invocation call for every value.
            var result = Channel.CreateUnbounded<WriteTagValueResult>();

            _ = Task.Run(async () => {
                try {
                    await foreach (var val in channel.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                        if (cancellationToken.IsCancellationRequested) {
                            break;
                        }
                        if (val == null) {
                            continue;
                        }

                        var writeResult = await connection.InvokeAsync<WriteTagValueResult>(
                            "WriteSnapshotTagValue",
                            adapterId,
                            request,
                            val,
                            cancellationToken
                        ).ConfigureAwait(false);

                        await result.Writer.WriteAsync(writeResult, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (Exception e) {
                    result.Writer.TryComplete(e);
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, cancellationToken);

            while (await result.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (result.Reader.TryRead(out var item)) {
                    yield return item;
                }
            }
        }


        /// <summary>
        /// Writes a stream of tag values to an adapter's history archive.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter to query.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="channel">
        ///   The channel that will emit the items to write.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the write result for each 
        ///   tag value back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async IAsyncEnumerable<WriteTagValueResult> WriteHistoricalTagValuesAsync(
            string adapterId, 
            WriteTagValuesRequest request,
            IAsyncEnumerable<WriteTagValueItem> channel, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken = default
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
#pragma warning disable CS0618 // Type or member is obsolete
            if (_client.CompatibilityLevel != CompatibilityLevel.AspNetCore2) {
                // We are using ASP.NET Core 3.0+ so we can use bidirectional streaming.
                await foreach (var item in connection.StreamAsync<WriteTagValueResult>(
                    "WriteHistoricalTagValues",
                    adapterId,
                    request,
                    channel,
                    cancellationToken
                ).ConfigureAwait(false)) {
                    yield return item;
                }
                yield break;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            // We are using ASP.NET Core 2.x, so we cannot use bidirectional streaming. Instead, 
            // we will read the channel ourselves and make an invocation call for every value.
            var result = Channel.CreateUnbounded<WriteTagValueResult>();

            _ = Task.Run(async () => {
                try {
                    await foreach (var val in channel.WithCancellation(cancellationToken).ConfigureAwait(false)) {
                        if (cancellationToken.IsCancellationRequested) {
                            break;
                        }
                        if (val == null) {
                            continue;
                        }

                        var writeResult = await connection.InvokeAsync<WriteTagValueResult>(
                            "WriteHistoricalTagValue",
                            adapterId,
                            request,
                            val,
                            cancellationToken
                        ).ConfigureAwait(false);

                        await result.Writer.WriteAsync(writeResult, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
                catch (Exception e) {
                    result.Writer.TryComplete(e);
                }
                finally {
                    result.Writer.TryComplete();
                }
            }, cancellationToken);

            while (await result.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false)) {
                while (result.Reader.TryRead(out var item)) {
                    yield return item;
                }
            }
        }

    }
}
