using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Client.Clients {
    public class TagValuesClient {

        private readonly AdapterSignalRClient _client;


        public TagValuesClient(AdapterSignalRClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<ChannelReader<TagValueQueryResult>> CreateSnapshotTagValueChannelAsync(string adapterId, IEnumerable<string> tags, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "CreateSnapshotTagValueChannel",
                adapterId,
                tags?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? new string[0],
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<TagIdentifier>> GetSnapshotTagValueChannelSubscriptionsAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagIdentifier>(
                "GetSnapshotTagValueChannelSubscriptions",
                adapterId,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<int> AddTagsToSnapshotTagValueChannelAsync(string adapterId, IEnumerable<string> tags, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<int>(
                "AddTagsToSnapshotTagValueChannel",
                adapterId,
                tags?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? new string[0],
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<int> RemoveTagsFromSnapshotTagValueChannelAsync(string adapterId, IEnumerable<string> tags, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (tags == null) {
                throw new ArgumentNullException(nameof(tags));
            }

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<int>(
                "RemoveTagsFromSnapshotTagValueChannel",
                adapterId,
                tags?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? new string[0],
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<TagValueQueryResult>> ReadSnapshotTagValuesAsync(string adapterId, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadSnapshotTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<TagValueQueryResult>> ReadRawTagValuesAsync(string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadRawTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<TagValueQueryResult>> ReadPlotTagValuesAsync(string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadPlotTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<TagValueQueryResult>> ReadInterpolatedTagValuesAsync(string adapterId, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadInterpolatedTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<TagValueQueryResult>> ReadTagValuesAtTimesAsync(string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<TagValueQueryResult>(
                "ReadTagValuesAtTimes",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctionsAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.InvokeAsync<IEnumerable<DataFunctionDescriptor>>(
                "GetSupportedDataFunctions",
                adapterId,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<ProcessedTagValueQueryResult>> ReadProcessedTagValuesAsync(string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            _client.ValidateObject(request);

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<ProcessedTagValueQueryResult>(
                "ReadProcessedTagValues",
                adapterId,
                request,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<WriteTagValueResult>> WriteSnapshotTagValuesAsync(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<WriteTagValueResult>(
                "WriteSnapshotTagValues",
                adapterId,
                channel,
                cancellationToken
            ).ConfigureAwait(false);
        }


        public async Task<ChannelReader<WriteTagValueResult>> WriteHistoricalTagValuesAsync(string adapterId, ChannelReader<WriteTagValueItem> channel, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (channel == null) {
                throw new ArgumentNullException(nameof(channel));
            }

            var connection = await _client.GetHubConnection(cancellationToken).ConfigureAwait(false);
            return await connection.StreamAsChannelAsync<WriteTagValueResult>(
                "WriteHistoricalTagValues",
                adapterId,
                channel,
                cancellationToken
            ).ConfigureAwait(false);
        }

    }
}
