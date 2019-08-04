using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.RealTimeData.Models;

namespace DataCore.Adapter.Http.Clients {
    public class TagValuesClient {

        private const string UrlPrefix = "api/data-core/v1.0/tag-values";

        private readonly AdapterHttpClient _client;


        public TagValuesClient(AdapterHttpClient client) {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }


        public async Task<IEnumerable<TagValueQueryResult>> ReadSnapshotValuesAsync(string adapterId, ReadSnapshotTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request != null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/snapshot";
            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<TagValueQueryResult>> ReadRawValuesAsync(string adapterId, ReadRawTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request != null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/raw";
            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<TagValueQueryResult>> ReadPlotValuesAsync(string adapterId, ReadPlotTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request != null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/plot";
            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<TagValueQueryResult>> ReadInterpolatedValuesAsync(string adapterId, ReadInterpolatedTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request != null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/interpolated";
            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<TagValueQueryResult>> ReadValuesAtTimesAsync(string adapterId, ReadTagValuesAtTimesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request != null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/values-at-times";
            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<TagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<ProcessedTagValueQueryResult>> ReadProcessedValuesAsync(string adapterId, ReadProcessedTagValuesRequest request, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (request != null) {
                throw new ArgumentNullException(nameof(request));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/processed";
            var response = await _client.HttpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<ProcessedTagValueQueryResult>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<DataFunctionDescriptor>> GetSupportedDataFunctionsAsync(string adapterId, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/supported-aggregations";
            var response = await _client.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<DataFunctionDescriptor>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<WriteTagValueResult>> WriteSnapshotValuesAsync(string adapterId, IEnumerable<WriteTagValueItem> values, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            values = values.Where(x => x != null).ToArray();
            if (!values.Any()) {
                return new WriteTagValueResult[0];
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write/snapshot";

            var response = await _client.HttpClient.PostAsJsonAsync(url, values, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<WriteTagValueResult>>(cancellationToken).ConfigureAwait(false);
        }


        public async Task<IEnumerable<WriteTagValueResult>> WriteHistoricalValuesAsync(string adapterId, IEnumerable<WriteTagValueItem> values, CancellationToken cancellationToken = default) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(Resources.Error_ParameterIsRequired, nameof(adapterId));
            }
            if (values == null) {
                throw new ArgumentNullException(nameof(values));
            }

            values = values.Where(x => x != null).ToArray();
            if (!values.Any()) {
                return new WriteTagValueResult[0];
            }

            var url = UrlPrefix + $"/{Uri.EscapeDataString(adapterId)}/write/snapshot";

            var response = await _client.HttpClient.PostAsJsonAsync(url, values, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<IEnumerable<WriteTagValueResult>>(cancellationToken).ConfigureAwait(false);
        }

    }
}
