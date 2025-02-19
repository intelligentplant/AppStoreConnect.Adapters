using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Services;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Class that manages tag definitions on behalf of an adapter, using an 
    /// <see cref="IKeyValueStore"/> to persist the definitions.
    /// </summary>
    /// <remarks>
    ///   The <see cref="TagManager"/> must be initialised via a call to <see cref="InitAsync"/> 
    ///   before it can be used.
    /// </remarks>
    public partial class TagManager : FeatureBase, ITagSearch, IDisposable {
        
        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The logger for the tag manager.
        /// </summary>
        private readonly ILogger<TagManager> _logger;

        /// <summary>
        /// Holds the in-memory tag definitions indexed by ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, TagDefinition> _tagsById = new ConcurrentDictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds the in-memory tag definitions indexed by name.
        /// </summary>
        private readonly ConcurrentDictionary<string, TagDefinition> _tagsByName = new ConcurrentDictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The definitions of the available tag properties.
        /// </summary>
        private readonly IEnumerable<AdapterProperty> _tagPropertyDefinitions;

        /// <summary>
        /// The <see cref="IKeyValueStore"/> where the tag definitions are persisted.
        /// </summary>
        private readonly IKeyValueStore? _keyValueStore;

        /// <summary>
        /// Flags if the class has been initialised.
        /// </summary>
        private bool _isInitialised;

        /// <summary>
        /// Lazy task for initialising the tag manager.
        /// </summary>
        private readonly Lazy<Task> _initTask;

        /// <summary>
        /// Cancellation token source that will fire when the tag manager is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// An optional callback that will be invoked when a tag is added, updated, or deleted.
        /// </summary>
        private readonly Func<ConfigurationChange, CancellationToken, ValueTask>? _onConfigurationChange;

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService { get; }

        /// <summary>
        /// The number of tag definitions held by the <see cref="TagManager"/>.
        /// </summary>
        public int Count => _tagsById.Count;


        /// <summary>
        /// Creates a new <see cref="TagManager"/> object.
        /// </summary>
        /// <param name="keyValueStore">
        ///   The <see cref="IKeyValueStore"/> where the tag definitions will be persisted to. 
        ///   Specify <see langword="null"/> if persistence of tag definitions is not required.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> for the tag manager.
        /// </param>
        /// <param name="tagPropertyDefinitions">
        ///   The definitions for the properties that can be defined on tags managed by the 
        ///   <see cref="TagManager"/>.
        /// </param>
        /// <param name="onConfigurationChange">
        ///   An optional callback that will be invoked when a tag is added, updated, or deleted.
        /// </param>
        /// <param name="logger">
        ///   The logger for the tag manager.
        /// </param>
        public TagManager(
            IKeyValueStore? keyValueStore = null, 
            IBackgroundTaskService? backgroundTaskService = null, 
            IEnumerable<AdapterProperty>? tagPropertyDefinitions = null,
            Func<ConfigurationChange, CancellationToken, ValueTask>? onConfigurationChange = null,
            ILogger<TagManager>? logger = null
        ) {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TagManager>.Instance;
            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            _onConfigurationChange = onConfigurationChange;
            _keyValueStore = keyValueStore?.CreateScopedStore("tag-manager:");

            _tagPropertyDefinitions = tagPropertyDefinitions?.ToArray() ?? Array.Empty<AdapterProperty>();
            _initTask = new Lazy<Task>(() => InitAsyncCore(_disposedTokenSource.Token), LazyThreadSafetyMode.ExecutionAndPublication);
        }


        /// <summary>
        /// Creates a delegate compatible with the <see cref="TagManager"/> constructor that 
        /// forwards configuration changes to a <see cref="ConfigurationChanges"/> instance.
        /// </summary>
        /// <param name="configurationChanges">
        ///   The <see cref="ConfigurationChanges"/> instance to use.
        /// </param>
        /// <returns>
        ///   A delegate that can be passed to the <see cref="TagManager"/> constructor.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="configurationChanges"/> is <see langword="null"/>.
        /// </exception>
        public static Func<ConfigurationChange, CancellationToken, ValueTask> CreateConfigurationChangeDelegate(ConfigurationChanges configurationChanges) {
            if (configurationChanges == null) {
                throw new ArgumentNullException(nameof(configurationChanges));
            }

            return async (change, ct) => _ = await configurationChanges.ValueReceived(change, ct).ConfigureAwait(false);
        }


        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
        /// </summary>
        private void ThrowOnDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }


        /// <summary>
        /// Initialises the <see cref="TagManager"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will initialise the <see cref="TagManager"/>.
        /// </returns>
        /// <remarks>
        ///   Call <see cref="InitAsync"/> to eagerly initialise the <see cref="TagManager"/>. If 
        ///   <see cref="InitAsync"/> is not called, the <see cref="TagManager"/> will be 
        ///   initialised on the first call to configure or query tags.
        /// </remarks>
        public async ValueTask InitAsync(CancellationToken cancellationToken = default) {
            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Initialises the <see cref="TagManager"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will initialise the <see cref="TagManager"/>. <see cref="Task"/> 
        ///   is used because the resulting task is returned by the <see cref="_initTask"/> field 
        ///   and may be awaited multiple times.
        /// </returns>
        private async Task InitAsyncCore(CancellationToken cancellationToken) {
            ThrowOnDisposed();

            if (_isInitialised) {
                return;
            }

            if (_keyValueStore == null) {
                _isInitialised = true;
                return;
            }

            _tagsById.Clear();
            _tagsByName.Clear();

            // "tags" key contains an array of the defined tag IDs.
            var readResult = await _keyValueStore.ReadAsync<string[]>("tags").ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            var completed = true;

            try {
                if (readResult == null) {
                    return;
                }

                foreach (var tagId in readResult) {
                    if (cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    // "tags:{id}" key contains the the definition with ID {id}.
                    var tagReadResult = await _keyValueStore.ReadAsync<TagDefinition>(string.Concat("tags:", tagId)).ConfigureAwait(false);
                    if (tagReadResult == null) {
                        continue;
                    }

                    _tagsById[tagReadResult.Id] = tagReadResult;
                    _tagsByName[tagReadResult.Name] = tagReadResult;
                }
            }
            catch {
                completed = false;
                throw;
            }
            finally {
                _isInitialised = completed;
            }
        }


        /// <summary>
        /// Gets the tag definition with the specified name or ID.
        /// </summary>
        /// <param name="tagNameOrId">
        ///   The tag name or ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the matching tag definition.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="TagManager"/> has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The <see cref="TagManager"/> has not been initialised via a call to <see cref="InitAsync"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagNameOrId"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<TagDefinition?> GetTagAsync(string tagNameOrId, CancellationToken cancellationToken = default) {
            ThrowOnDisposed();

            if (tagNameOrId == null) {
                throw new ArgumentNullException(nameof(tagNameOrId));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            return GetTag(tagNameOrId);
        }


        /// <summary>
        /// Gets the tag definition with the specified name or ID.
        /// </summary>
        /// <param name="tagNameOrId">
        ///   The tag name or ID.
        /// </param>
        /// <returns>
        ///   The matching tag definition.
        /// </returns>
        private TagDefinition? GetTag(string tagNameOrId) {
            if (_tagsByName.TryGetValue(tagNameOrId, out var tag)) {
                return tag;
            }

            if (_tagsById.TryGetValue(tagNameOrId, out tag)) {
                return tag;
            }

            return null;
        }


        /// <summary>
        /// Invokes the <see cref="_onConfigurationChange"/> callback.
        /// </summary>
        /// <param name="tag">
        ///   The node that triggered the change.
        /// </param>
        /// <param name="changeType">
        ///   The change type.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will invoke the <see cref="_onConfigurationChange"/> 
        ///   callback.
        /// </returns>
        private async ValueTask OnConfigurationChangeAsync(TagDefinition tag, ConfigurationChangeType changeType, CancellationToken cancellationToken) {
            if (_onConfigurationChange == null) {
                return;
            }

            await _onConfigurationChange(new ConfigurationChange(ConfigurationChangeItemTypes.Tag, tag.Id, tag.Name, changeType, null), cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Adds or updates a tag definition.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will cache the tag definition and save it to the 
        ///   <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="TagManager"/> has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The <see cref="TagManager"/> has not been initialised via a call to <see cref="InitAsync"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tag"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask AddOrUpdateTagAsync(TagDefinition tag, CancellationToken cancellationToken = default) {
            ThrowOnDisposed();

            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            if (_keyValueStore != null) {
                // "tags:{id}" key contains the the definition with ID {id}.
                await _keyValueStore.WriteAsync(string.Concat("tags:", tag.Id), tag).ConfigureAwait(false);
            }

            // Check if we are renaming the tag.
            if (_tagsById.TryGetValue(tag.Id, out var oldTag) && !string.Equals(oldTag.Name, tag.Name, StringComparison.OrdinalIgnoreCase)) {
                // Name has changed; remove lookup for old tag name.
                _tagsByName.TryRemove(oldTag.Name, out _);
            }

            // Flags if the keys in _tagsById have been modified by this operation. We will
            // assume that they have by default, and then set to false if we are doing an
            // update on an existing tag, to prevent us from updating the list of tag IDs in
            // the data store unless we have to.
            var indexHasChanged = true;

            // Add/update entry in _tagsById lookup.
            _ = _tagsById.AddOrUpdate(tag.Id, tag, (key, existing) => {
                // This is an update of an existing entry.
                indexHasChanged = false;
                return tag;
            });

            // Add/update entry in _tagsByName lookup.
            _tagsByName[tag.Name] = tag;

            if (indexHasChanged) {
                LogCreatedTag(tag.Id, tag.Name);
                if (_keyValueStore != null) {
                    // "tags" key contains an array of the defined tag IDs.
                    await _keyValueStore.WriteAsync("tags", _tagsById.Keys.ToArray()).ConfigureAwait(false);
                }
                await OnConfigurationChangeAsync(tag, ConfigurationChangeType.Created, cancellationToken).ConfigureAwait(false);
            }
            else {
                LogUpdatedTag(tag.Id, tag.Name);
                await OnConfigurationChangeAsync(tag, ConfigurationChangeType.Updated, cancellationToken).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Deletes a tag from the <see cref="TagManager"/> cache and the underlying 
        /// <see cref="IKeyValueStore"/>.
        /// </summary>
        /// <param name="tagNameOrId">
        ///   The name or ID of the tag to delete.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="TagManager"/> has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The <see cref="TagManager"/> has not been initialised via a call to <see cref="InitAsync"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagNameOrId"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> DeleteTagAsync(string tagNameOrId, CancellationToken cancellationToken = default) {
            ThrowOnDisposed();

            if (tagNameOrId == null) {
                throw new ArgumentNullException(nameof(tagNameOrId));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            var tag = GetTag(tagNameOrId);
            if (tag == null) {
                return false;
            }

            // "tags:{id}" key contains the the definition with ID {id}.
            var result = _keyValueStore == null 
                ? true 
                : await _keyValueStore.DeleteAsync(string.Concat("tags:", tag.Id)).ConfigureAwait(false);

            if (result) {
                _tagsById.TryRemove(tag.Id, out _);
                _tagsByName.TryRemove(tag.Name, out _);

                LogDeletedTag(tag.Id, tag.Name);
                await OnConfigurationChangeAsync(tag, ConfigurationChangeType.Deleted, cancellationToken).ConfigureAwait(false);

                if (_keyValueStore != null) {
                    // "tags" key contains an array of the defined tag IDs.
                    await _keyValueStore.WriteAsync("tags", _tagsById.Keys.ToArray()).ConfigureAwait(false);
                }
            }

            return result;
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<TagDefinition> FindTags(
            IAdapterCallContext context, 
            FindTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            ThrowOnDisposed();

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            foreach (var item in _tagsById.Values.ApplyFilter(request)) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                yield return item.Clone(request.ResultFields);
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<TagDefinition> GetTags(
            IAdapterCallContext context, 
            GetTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            ThrowOnDisposed();
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            foreach (var item in request.Tags ?? Array.Empty<string>()) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                if (item == null) {
                    continue;
                }

                var tag = GetTag(item);
                if (tag != null) {
                    yield return tag;
                }
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            GetTagPropertiesRequest request,
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            ThrowOnDisposed();
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            foreach (var item in _tagPropertyDefinitions.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).SelectPage(request)) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                yield return item;
            }
        }


        /// <inheritdoc/>
        protected override IEnumerable<KeyValuePair<string, string>> GetFeatureHealthCheckData(IAdapterCallContext context) {
            foreach (var item in base.GetFeatureHealthCheckData(context)) {
                yield return item;
            }

            yield return new KeyValuePair<string, string>(Resources.HealthChecks_Data_TagCount, _tagsById.Count.ToString(context?.CultureInfo));
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();
            _tagsById.Clear();
            _tagsByName.Clear();
            _disposed = true;

            GC.SuppressFinalize(this);
        }


        [LoggerMessage(1, LogLevel.Debug, "Created tag '{name}' (ID: '{id}')")]
        partial void LogCreatedTag(string id, string name);

        [LoggerMessage(2, LogLevel.Debug, "Updated tag '{name}' (ID: '{id}')")]
        partial void LogUpdatedTag(string id, string name);

        [LoggerMessage(3, LogLevel.Debug, "Deleted tag '{name}' (ID: '{id}')")]
        partial void LogDeletedTag(string id, string name);

    }
}
