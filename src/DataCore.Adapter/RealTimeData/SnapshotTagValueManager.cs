using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Json;
using DataCore.Adapter.Services;
using DataCore.Adapter.Tags;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// <see cref="IReadSnapshotTagValues"/> and <see cref="ISnapshotTagValuePush"/> provider that 
    /// uses an <see cref="IKeyValueStore"/> to persist snapshot tag values between adapter or host 
    /// restarts.
    /// </summary>
    public class SnapshotTagValueManager : SnapshotTagValuePush, IReadSnapshotTagValues {

        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The <see cref="IKeyValueStore"/> to use.
        /// </summary>
        private readonly IKeyValueStore _keyValueStore;

        /// <summary>
        /// Options for serializing/deserializing tag values.
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions();

        /// <summary>
        /// Cached tag values, indexed by tag ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, TagValueQueryResult> _valuesById = new ConcurrentDictionary<string, TagValueQueryResult>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Cached tag values, indexed by tag name.
        /// </summary>
        private readonly ConcurrentDictionary<string, TagValueQueryResult> _valuesByName = new ConcurrentDictionary<string, TagValueQueryResult>(StringComparer.OrdinalIgnoreCase);


        /// <summary>
        /// Creates a new <see cref="SnapshotTagValueManager"/> object.
        /// </summary>
        /// <param name="options">
        ///   The options for the <see cref="SnapshotTagValueManager"/>.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> to use when running snapshot subscriptions.
        /// </param>
        /// <param name="keyValueStore">
        ///   The <see cref="IKeyValueStore"/> to persist snapshot values to.
        /// </param>
        /// <param name="logger">
        ///   The <see cref="ILogger"/> to use.
        /// </param>
        public SnapshotTagValueManager(
            SnapshotTagValueManagerOptions? options,
            IBackgroundTaskService? backgroundTaskService,
            IKeyValueStore keyValueStore,
            ILogger? logger
        ) : base(options, backgroundTaskService, logger) {
            if (keyValueStore == null) {
                throw new ArgumentNullException(nameof(keyValueStore));
            }

            _keyValueStore = keyValueStore.CreateScopedStore("snapshot-tag-value-manager:");
            _jsonOptions.AddDataCoreAdapterConverters();
        }


        private async ValueTask<TagValueQueryResult?> ReadValueAsync(string tagNameOrId) {
            if (_valuesByName.TryGetValue(tagNameOrId, out var sample)) {
                return sample;
            }
            if (_valuesById.TryGetValue(tagNameOrId, out sample)) {
                return sample;
            }

            async ValueTask<string> GetTagId() {
                var nameToIdResult = await _keyValueStore.ReadJsonAsync<string>($"name-to-id:{tagNameOrId}", _jsonOptions).ConfigureAwait(false);
                if (nameToIdResult.Status != KeyValueStoreOperationStatus.OK || string.IsNullOrEmpty(nameToIdResult.Value)) {
                    // Assume that tagNameOrId is the tag ID.
                    return tagNameOrId;
                }

                return nameToIdResult.Value!;
            }

            var tagId = await GetTagId().ConfigureAwait(false);
            var valueResult = await _keyValueStore.ReadJsonAsync<TagValueQueryResult>($"value:{tagId}", _jsonOptions).ConfigureAwait(false);

            if (valueResult.Status == KeyValueStoreOperationStatus.OK && valueResult.Value != null) {
                // Update lookups.
                _valuesById[valueResult.Value.TagId] = valueResult.Value;
                _valuesById[valueResult.Value.TagName] = valueResult.Value;
            }

            return valueResult.Value;
        }


        /// <inheritdoc/>
        public override async ValueTask<bool> ValueReceived(TagValueQueryResult message, CancellationToken cancellationToken = default) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            return await ValueReceivedCore(message, true, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Emits a sample to subscribers and optionally updates the cache entry for the sample's 
        /// tag.
        /// </summary>
        /// <param name="sample">
        ///   The sample. 
        /// </param>
        /// <param name="updateCache">
        ///   When <see langword="true"/>, the cache entries associated with the sample will be 
        ///   updated.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that returns a flag indicating if the sample was 
        ///   pushed to any subscribers.
        /// </returns>
        private async ValueTask<bool> ValueReceivedCore(TagValueQueryResult sample, bool updateCache, CancellationToken cancellationToken) {
            if (updateCache) {
                _valuesByName[sample.TagId] = sample;
                _valuesByName[sample.TagName] = sample;

                await _keyValueStore.WriteJsonAsync($"value:{sample.TagId}", sample, _jsonOptions).ConfigureAwait(false);
                await _keyValueStore.WriteJsonAsync($"name-to-id:{sample.TagName}", sample.TagId, _jsonOptions).ConfigureAwait(false);
            }

            return await base.ValueReceived(sample, cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async Task OnTagsAdded(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            await base.OnTagsAdded(tags, cancellationToken).ConfigureAwait(false);
            foreach (var item in tags) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                var sample = await ReadValueAsync(item.Id).ConfigureAwait(false);
                if (sample != null) {
                    await ValueReceivedCore(sample, false, cancellationToken).ConfigureAwait(false);
                }
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<TagValueQueryResult> ReadSnapshotTagValues(
            IAdapterCallContext context, 
            ReadSnapshotTagValuesRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            await Task.Yield();

            foreach (var item in request.Tags ?? Array.Empty<string>()) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                if (string.IsNullOrWhiteSpace(item)) {
                    continue;
                }

                var sample = await ReadValueAsync(item).ConfigureAwait(false);
                if (sample != null) {
                    yield return sample;
                }
            }
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing && !_disposed) {
                _valuesById.Clear();
                _valuesByName.Clear();
            }

            _disposed = true;
        }

    }


    /// <summary>
    /// Options for <see cref="SnapshotTagValueManager"/>.
    /// </summary>
    public class SnapshotTagValueManagerOptions : SnapshotTagValuePushOptions { }

}
