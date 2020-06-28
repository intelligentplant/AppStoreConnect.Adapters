using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return the ID of the subscription. This can 
        ///   be used to subscribe to individual tags via the <see cref="CreateSnapshotTagValueChannelAsync"/> 
        ///   method.
        /// </returns>
        public async Task<string> CreateSnapshotTagValueSubscriptionAsync(
            string adapterId,
            CreateSnapshotTagValueSubscriptionRequest request,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<string>(
                "CreateSnapshotTagValueSubscription",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes a snapshot tag value subscription.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task{TResult}"/> that will return a flag indicating if the subscription 
        ///   was deleted. Deleting a subscription will cancel all active tag value channels 
        ///   associated with the subscription.
        /// </returns>
        public async Task<bool> DeleteSnapshotTagValueSubscriptionAsync(
            string subscriptionId,
            CancellationToken cancellationToken
        ) {
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(subscriptionId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<bool>(
                "DeleteSnapshotTagValueSubscription",
                subscriptionId,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Creates a subscription channel to receive snapshot values in real-time from the 
        /// specified adapter.
        /// </summary>
        /// <param name="subscriptionId">
        ///   The subscription ID to add the tag to.
        /// </param>
        /// <param name="tagIdOrName">
        ///   The tag ID or name to subscribe to.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation. If this token fires, or the connection is
        ///   lost, the channel will be closed.
        /// </param>
        /// <returns>
        ///   A task that will return a channel that is used to stream the snapshot values back to 
        ///   the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="subscriptionId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tagIdOrName"/> is <see langword="null"/> or white space.
        /// </exception>
        public async Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannelAsync(
            string subscriptionId, 
            string tagIdOrName,
            CancellationToken cancellationToken = default
        ) {
            if (string.IsNullOrWhiteSpace(subscriptionId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(subscriptionId));
            }
            if (string.IsNullOrWhiteSpace(tagIdOrName)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(tagIdOrName));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "CreateSnapshotTagValueChannel",
                subscriptionId,
                tagIdOrName,
                cancellationToken
            ).ConfigureAwait(false);
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
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
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
        public async Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValuesAsync(string adapterId, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadSnapshotTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
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
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
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
        public async Task<ChannelReader<TagValueQueryResult>> ReadRawTagValuesAsync(string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadRawTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
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
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
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
        public async Task<ChannelReader<TagValueQueryResult>> ReadPlotTagValuesAsync(string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadPlotTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
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
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
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
        public async Task<ChannelReader<TagValueQueryResult>> ReadTagValuesAtTimesAsync(string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadTagValuesAtTimes",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
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
        ///   A task that will return descriptors for the supported data functions.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        public async Task<ChannelReader<DataFunctionDescriptor>> GetSupportedDataFunctionsAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<DataFunctionDescriptor>(
                "GetSupportedDataFunctions",
                adapterId,
                cancellationToken
            ).ConfigureAwait(false);
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
        ///   A task that will return a channel that is used to stream the results back to the 
        ///   caller.
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
        public async Task<ChannelReader<ProcessedTagValueQueryResult>> ReadProcessedTagValuesAsync(string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<ProcessedTagValueQueryResult>(
                "ReadProcessedTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a stream of tag values to an adapter's snapshot.
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
        ///   tag value back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async Task<ChannelReader<WriteTagValueResult>> WriteSnapshotTagValuesAsync(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<WriteTagValueResult>(
                "WriteSnapshotTagValues",
                adapterId,
                channel,
                cancellationToken
            ).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes a stream of tag values to an adapter's history archive.
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
        ///   tag value back to the caller.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="adapterId"/> is <see langword="null"/> or white space.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="channel"/> is <see langword="null"/>.
        /// </exception>
        public async Task<ChannelReader<WriteTagValueResult>> WriteHistoricalTagValuesAsync(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(true, cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<WriteTagValueResult>(
                "WriteHistoricalTagValues",
                adapterId,
                channel,
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
