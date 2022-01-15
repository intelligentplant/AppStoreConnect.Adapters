using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    public class SnapshotTagValueManager : SnapshotTagValuePushBase, IReadSnapshotTagValues {

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
        /// Lazy init method.
        /// </summary>
        private readonly Lazy<Task> _init;


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

            _init = new Lazy<Task>(() => InitAsync(DisposedToken), LazyThreadSafetyMode.ExecutionAndPublication);
        }


        /// <summary>
        /// Initialises the <see cref="SnapshotTagValueManager"/> by loading values for known tags 
        /// into the in-memory cache.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will perform the operation.
        /// </returns>
        private async Task InitAsync(CancellationToken cancellationToken) {
            var tagIds = await LoadTagsIdsAsync().ConfigureAwait(false);
            if (tagIds == null || cancellationToken.IsCancellationRequested) {
                return;
            }

            foreach (var tagId in tagIds) {
                if (cancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(tagId)) {
                    continue;
                }

                var value = await LoadTagValueAsync(tagId).ConfigureAwait(false);
                if (value == null) {
                    continue;
                }

                _valuesById[tagId] = value;
            }
        }


        /// <summary>
        /// Loads tag IDs that have associated snapshot values from the data store.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the tag IDs.
        /// </returns>
        private async ValueTask<string[]?> LoadTagsIdsAsync() {
            return await _keyValueStore.ReadJsonAsync<string[]>("tags", _jsonOptions).ConfigureAwait(false);
        }


        /// <summary>
        /// Saves tag IDs that have associated snapshot values to the data store.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will save the tag IDs.
        /// </returns>
        private async ValueTask SaveTagsIdsAsync() {
            await _keyValueStore.WriteJsonAsync("tags", _valuesById.Keys.ToArray(), _jsonOptions).ConfigureAwait(false);
        }


        /// <summary>
        /// Loads the snapshot value for the specified tag from the data store.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the value.
        /// </returns>
        private async ValueTask<TagValueQueryResult?> LoadTagValueAsync(string tagId) {
            return await _keyValueStore.ReadJsonAsync<TagValueQueryResult>($"value:{tagId}", _jsonOptions).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes the snapshot value for the specified tag to the data store.
        /// </summary>
        /// <param name="value">
        ///   The value.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will save the value.
        /// </returns>
        private async ValueTask SaveTagValueAsync(TagValueQueryResult value) {
            await _keyValueStore.WriteJsonAsync($"value:{value.TagId}", value, _jsonOptions).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes the snapshot value for the specified tag from the data store.
        /// </summary>
        /// <param name="tagId">
        ///   The tag ID.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will delete the value.
        /// </returns>
        private async ValueTask DeleteTagValueAsync(string tagId) {
            await _keyValueStore.DeleteAsync($"value:{tagId}").ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the snapshot value for the specified tag from the cache, or from the underlying 
        /// key-value store.
        /// </summary>
        /// <param name="tagIdentifier">
        ///   The tag identifier.
        /// </param>
        /// <returns>
        ///   The snapshot value, if available.
        /// </returns>
        private TagValueQueryResult? GetSnapshotTagValue(TagIdentifier tagIdentifier) {
            if (_valuesById.TryGetValue(tagIdentifier.Id, out var value)) {
                return value;
            }

            return null;
        }


        /// <inheritdoc/>
        protected override async Task OnTagsAdded(IEnumerable<TagIdentifier> tags, CancellationToken cancellationToken) {
            await base.OnTagsAdded(tags, cancellationToken).ConfigureAwait(false);

            await _init.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            foreach (var item in tags) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                var sample = GetSnapshotTagValue(item);
                if (sample != null) {
                    await PublishValueToSubscribersAsync(sample, cancellationToken).ConfigureAwait(false);
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

            if (request.Tags == null) {
                yield break;
            }

            await _init.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            await foreach (var tag in ResolveTags(context, request.Tags, cancellationToken).ConfigureAwait(false)) {
                var sample = GetSnapshotTagValue(tag);
                if (sample != null) {
                    yield return sample;
                }
            }
        }


        /// <summary>
        /// Deletes the snapshot value for a tag.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that returns <see langword="true"/> if the value 
        ///   for the tag was deleted, or <see langword="false"/> if the tag did not have a 
        ///   snapshot value to delete.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="SnapshotTagValueManager"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> DeleteSnapshotTagValueAsync(TagIdentifier tag, CancellationToken cancellationToken) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            await _init.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            if (!_valuesById.TryRemove(tag.Id, out _)) {
                return false;
            }

            await SaveTagsIdsAsync().ConfigureAwait(false);
            await DeleteTagValueAsync(tag.Id).ConfigureAwait(false);

            return true;
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<TagIdentifier> ResolveTags(
            IAdapterCallContext context,
            IEnumerable<string> tags,
            [EnumeratorCancellation] CancellationToken cancellationToken
        ) {
            if (Options.TagResolver == null) {
                // No tag resolver defined; we will try and resolve using the in-memory values in
                // the cache.
                await _init.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

                foreach (var value in _valuesById.Values.Where(x => tags.Contains(x.TagName, StringComparer.OrdinalIgnoreCase) || tags.Contains(x.TagId, StringComparer.OrdinalIgnoreCase))) {
                    yield return new TagIdentifier(value.TagId, value.TagName);
                }

                yield break;
            }

            // Tag resolver defined; delegate to the base implementation.
            await foreach (var item in base.ResolveTags(context, tags, cancellationToken).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<bool> AddOrUpdateCachedValueAsync(TagIdentifier tag, TagValueQueryResult value, CancellationToken cancellationToken) {
            await _init.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            var isNewValue = true;
            _valuesById.AddOrUpdate(tag.Id, value, (id, val) => {
                isNewValue = false;
                return value;
            });

            await SaveTagValueAsync(value).ConfigureAwait(false);
            if (isNewValue) {
                await SaveTagsIdsAsync().ConfigureAwait(false);
            }

            return true;
        }


        /// <inheritdoc/>
        protected override async ValueTask<TagValueQueryResult?> GetCachedValueAsync(TagIdentifier tag, CancellationToken cancellationToken) {
            await _init.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
            return GetSnapshotTagValue(tag);
        }


        /// <inheritdoc/>
        protected override ValueTask<bool> RemoveCachedValueAsync(TagIdentifier tag, CancellationToken cancellationToken) {
            // Always return false; cached values can only be deleted by explicit calls to
            // DeleteSnapshotTagValueAsync.
            return new ValueTask<bool>(false);
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing && !_disposed) {
                _valuesById.Clear();
            }

            _disposed = true;
        }

    }


    /// <summary>
    /// Options for <see cref="SnapshotTagValueManager"/>.
    /// </summary>
    public class SnapshotTagValueManagerOptions : SnapshotTagValuePushOptions { }

}
