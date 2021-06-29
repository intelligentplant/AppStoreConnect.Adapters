using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Common;
using DataCore.Adapter.Services;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.Tags {

    /// <summary>
    /// Class that manages tag definitions on behalf of an adapter, using an 
    /// <see cref="IKeyValueStore"/> to persist the definitions.
    /// </summary>
    /// <remarks>
    ///   The <see cref="TagManager"/> must be initialised via a call to <see cref="InitAsync"/> 
    ///   before it can be used.
    /// </remarks>
    public class TagManager : ITagSearch, IDisposable {
        
        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _disposed;

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
        private readonly IKeyValueStore _keyValueStore;

        /// <summary>
        /// Flags if the class has been initialised.
        /// </summary>
        private bool _isInitialised;

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService { get; }


        /// <summary>
        /// Creates a new <see cref="TagManager"/> object.
        /// </summary>
        /// <param name="keyValueStore">
        ///   The <see cref="IKeyValueStore"/> where the tag definitions will be persisted to.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> for the tag manager.
        /// </param>
        /// <param name="tagPropertyDefinitions">
        ///   The definitions for the properties that can be defined on tags managed by the 
        ///   <see cref="TagManager"/>.
        /// </param>
        public TagManager(
            IKeyValueStore keyValueStore, 
            IBackgroundTaskService? backgroundTaskService = null, 
            IEnumerable<AdapterProperty>? tagPropertyDefinitions = null
        ) {
            if (keyValueStore == null) {
                throw new ArgumentNullException(nameof(keyValueStore));
            }

            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            _keyValueStore = keyValueStore.CreateScopedStore("tag-manager:");

            _tagPropertyDefinitions = tagPropertyDefinitions?.ToArray() ?? Array.Empty<AdapterProperty>();
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
        /// Throws an <see cref="InvalidOperationException"/> if the object has not been initialised.
        /// </summary>
        private void ThrowOnNotInitialised() {
            if (!_isInitialised) {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.Error_TagManager_NotInitialised, nameof(InitAsync)));
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
        public async ValueTask InitAsync(CancellationToken cancellationToken = default) {
            ThrowOnDisposed();

            _isInitialised = false;
            _tagsById.Clear();
            _tagsByName.Clear();

            // "tags" key contains an array of the defined tag IDs.
            var readResult = await _keyValueStore.ReadAsync<string[]>("tags").ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            var completed = true;

            try {
                if (readResult.Value == null) {
                    return;
                }

                foreach (var tagId in readResult.Value) {
                    if (cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    // "tags:{id}" key contains the the definition with ID {id}.
                    var tagReadResult = await _keyValueStore.ReadAsync<TagDefinition>(string.Concat("tags:", tagId)).ConfigureAwait(false);
                    if (tagReadResult.Value == null) {
                        continue;
                    }

                    _tagsById[tagReadResult.Value.Id] = tagReadResult.Value;
                    _tagsByName[tagReadResult.Value.Name] = tagReadResult.Value;
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
        /// Adds or updates a tag definition.
        /// </summary>
        /// <param name="tag">
        ///   The tag.
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
        public async ValueTask AddOrUpdateTagAsync(TagDefinition tag) {
            ThrowOnDisposed();
            ThrowOnNotInitialised();

            if (tag == null) {
                throw new ArgumentNullException(nameof(tag));
            }

            // "tags:{id}" key contains the the definition with ID {id}.
            var result = await _keyValueStore.WriteAsync(string.Concat("tags:", tag.Id), tag).ConfigureAwait(false);
            if (result == KeyValueStoreOperationStatus.OK) {
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
                    // "tags" key contains an array of the defined tag IDs.
                    await _keyValueStore.WriteAsync("tags", _tagsById.Keys.ToArray()).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// Deletes a tag from the <see cref="TagManager"/> cache and the underlying 
        /// <see cref="IKeyValueStore"/>.
        /// </summary>
        /// <param name="tagId"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="TagManager"/> has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The <see cref="TagManager"/> has not been initialised via a call to <see cref="InitAsync"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="tagId"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> DeleteTagAsync(string tagId) {
            ThrowOnDisposed();
            ThrowOnNotInitialised();

            if (tagId == null) {
                throw new ArgumentNullException(nameof(tagId));
            }

            // "tags:{id}" key contains the the definition with ID {id}.
            var result = await _keyValueStore.DeleteAsync(string.Concat("tags:", tagId)).ConfigureAwait(false);
            if (result == KeyValueStoreOperationStatus.OK && _tagsById.TryRemove(tagId, out _)) {
                // "tags" key contains an array of the defined tag IDs.
                await _keyValueStore.WriteAsync("tags", _tagsById.Keys.ToArray()).ConfigureAwait(false);
            }

            return result == KeyValueStoreOperationStatus.OK;
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<TagDefinition> FindTags(
            IAdapterCallContext context, 
            FindTagsRequest request, 
            [EnumeratorCancellation]
            CancellationToken cancellationToken
        ) {
            ThrowOnDisposed();
            ThrowOnNotInitialised();
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            await Task.Yield();

            foreach (var item in _tagsById.Values.ApplyFilter(request)) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                yield return item;
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
            ThrowOnNotInitialised();
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            await Task.Yield();

            TagDefinition? GetTag(string tagNameOrId) {
                if (_tagsByName.TryGetValue(tagNameOrId, out var tag)) {
                    return tag;
                }

                if (_tagsById.TryGetValue(tagNameOrId, out tag)) {
                    return tag;
                }

                return null;
            }

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
        public IAsyncEnumerable<AdapterProperty> GetTagProperties(
            IAdapterCallContext context, 
            GetTagPropertiesRequest request,
            CancellationToken cancellationToken
        ) {
            ThrowOnDisposed();
            ThrowOnNotInitialised();
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            return _tagPropertyDefinitions
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .SelectPage(request)
                .ToAsyncEnumerable(cancellationToken);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _tagsById.Clear();
            _tagsByName.Clear();
            _disposed = true;

            GC.SuppressFinalize(this);
        }

    }
}
