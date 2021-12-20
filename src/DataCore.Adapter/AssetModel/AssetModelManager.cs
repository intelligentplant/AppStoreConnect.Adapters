using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using IntelligentPlant.BackgroundTasks;

namespace DataCore.Adapter.AssetModel {

    /// <summary>
    /// Class that manages an asset model on behalf of an adapter, using an 
    /// <see cref="IKeyValueStore"/> to persist the definitions.
    /// </summary>
    /// <remarks>
    ///   The <see cref="AssetModelManager"/> must be initialised via a call to <see cref="InitAsync"/> 
    ///   before it can be used.
    /// </remarks>
    public class AssetModelManager : IAssetModelBrowse, IAssetModelSearch, IDisposable {

        /// <summary>
        /// Indicates if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Holds the in-memory node definitions indexed by ID.
        /// </summary>
        private readonly ConcurrentDictionary<string, AssetModelNode> _nodesById = new ConcurrentDictionary<string, AssetModelNode>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The <see cref="IKeyValueStore"/> where the node definitions are persisted.
        /// </summary>
        private readonly IKeyValueStore _keyValueStore;

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
        /// Lock for create/update/delete operations.
        /// </summary>
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        /// <inheritdoc/>
        public IBackgroundTaskService BackgroundTaskService { get; }


        /// <summary>
        /// Creates a new <see cref="AssetModelManager"/> object.
        /// </summary>
        /// <param name="keyValueStore">
        ///   The <see cref="IKeyValueStore"/> where the nodes will be persisted to.
        /// </param>
        /// <param name="backgroundTaskService">
        ///   The <see cref="IBackgroundTaskService"/> for the asset model manager.
        /// </param>
        public AssetModelManager(
            IKeyValueStore keyValueStore,
            IBackgroundTaskService? backgroundTaskService = null
        ) {
            if (keyValueStore == null) {
                throw new ArgumentNullException(nameof(keyValueStore));
            }

            BackgroundTaskService = backgroundTaskService ?? IntelligentPlant.BackgroundTasks.BackgroundTaskService.Default;
            _keyValueStore = keyValueStore.CreateScopedStore("asset-model-manager:");

            _initTask = new Lazy<Task>(() => InitAsyncCore(_disposedTokenSource.Token), LazyThreadSafetyMode.ExecutionAndPublication);
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
        /// Initialises the <see cref="AssetModelManager"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will initialise the <see cref="AssetModelManager"/>.
        /// </returns>
        /// <remarks>
        ///   Call <see cref="InitAsync"/> to eagerly initialise the <see cref="AssetModelManager"/>. If 
        ///   <see cref="InitAsync"/> is not called, the <see cref="AssetModelManager"/> will be 
        ///   initialised on the first call to configure or query tags.
        /// </remarks>
        public async ValueTask InitAsync(CancellationToken cancellationToken = default) {
            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Initialises the <see cref="AssetModelManager"/>.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will initialise the <see cref="AssetModelManager"/>. 
        ///   <see cref="Task"/> is used because the resulting task is returned by the <see cref="_initTask"/> 
        ///   field and may be awaited multiple times.
        /// </returns>
        private async Task InitAsyncCore(CancellationToken cancellationToken) {
            ThrowOnDisposed();

            if (_isInitialised) {
                return;
            }

            _nodesById.Clear();

            // "nodes" key contains an array of the defined node IDs.
            var readResult = await _keyValueStore.ReadAsync<string[]>("nodes").ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            var completed = true;

            try {
                if (readResult.Value == null) {
                    return;
                }

                foreach (var nodeId in readResult.Value) {
                    if (cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    // "nodes:{id}" key contains the the definition with ID {id}.
                    var nodeReadResult = await _keyValueStore.ReadAsync<AssetModelNode>(string.Concat("nodes:", nodeId)).ConfigureAwait(false);
                    if (nodeReadResult.Value == null) {
                        continue;
                    }

                    _nodesById[nodeReadResult.Value.Id] = nodeReadResult.Value;
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
        /// Gets a node by ID.
        /// </summary>
        /// <param name="nodeId">
        ///   The node ID.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   The matching <see cref="AssetModelNode"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="AssetModelManager"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="nodeId"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<AssetModelNode?> GetNodeAsync(string nodeId, CancellationToken cancellationToken = default) {
            ThrowOnDisposed();

            if (nodeId == null) {
                throw new ArgumentNullException(nameof(nodeId));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            return GetNode(nodeId);
        }



        private AssetModelNode? GetNode(string nodeId) {
            return _nodesById.TryGetValue(nodeId, out var node) 
                ? node 
                : null;
        }


        /// <summary>
        /// Adds or updates a node definition.
        /// </summary>
        /// <param name="node">
        ///   The node.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will cache the node definition and save it to the 
        ///   <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="AssetModelManager"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="node"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask AddOrUpdateNodeAsync(AssetModelNode node, CancellationToken cancellationToken = default) {
            ThrowOnDisposed();

            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            var hasChildren = HasRegisteredChildren(node.Id);
            if (hasChildren != node.HasChildren) {
                node = new AssetModelNodeBuilder(node).WithChildren(hasChildren).Build();
            }

            await AddOrUpdateNodeCoreAsync(node, true, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Adds or updates a node definition.
        /// </summary>
        /// <param name="node">
        ///   The node.
        /// </param>
        /// <param name="requiresLock">
        ///   <see langword="true"/> if <see cref="_writeLock"/> must be acquired before updating, 
        ///   or <see langword="false"/> if it has already been acquired.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will cache the node definition and save it to the 
        ///   <see cref="IKeyValueStore"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="node"/> is <see langword="null"/>.
        /// </exception>
        private async ValueTask AddOrUpdateNodeCoreAsync(AssetModelNode node, bool requiresLock, CancellationToken cancellationToken) {
            if (requiresLock) {
                await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            try {
                // "nodes:{id}" key contains the definition with ID {id}.
                var result = await _keyValueStore.WriteAsync(string.Concat("nodes:", node.Id), node).ConfigureAwait(false);
                if (result == KeyValueStoreOperationStatus.OK) {
                    _nodesById.TryRemove(node.Id, out _);

                    // Flags if the keys in _nodesById have been modified by this operation. We will
                    // assume that they have by default, and then set to false if we are doing an
                    // update on an existing tag, to prevent us from updating the list of node IDs in
                    // the data store unless we have to.
                    var indexHasChanged = true;

                    // Add/update entry in _nodesById lookup.
                    _ = _nodesById.AddOrUpdate(node.Id, node, (key, existing) => {
                        // This is an update of an existing entry.
                        indexHasChanged = false;
                        return node;
                    });

                    if (indexHasChanged) {
                        // "nodes" key contains an array of the defined node IDs.
                        await _keyValueStore.WriteAsync("nodes", _nodesById.Keys.ToArray()).ConfigureAwait(false);
                    }
                }

                if (node.Parent != null) {
                    // If this node's parent does not have its HasParent flag set, we need to update
                    // that as well.
                    var parent = GetNode(node.Parent);
                    if (parent != null && !parent.HasChildren) {
                        await AddOrUpdateNodeCoreAsync(new AssetModelNodeBuilder(parent).WithChildren(true).Build(), false, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally {
                if (requiresLock) {
                    _writeLock.Release();
                }
            }
        }


        /// <summary>
        /// Tests if any nodes have a <see cref="AssetModelNode.Parent"/> property that matches 
        /// the specified node ID.
        /// </summary>
        /// <param name="nodeId">
        ///   The parent node ID.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if any registered nodes specify the <paramref name="nodeId"/> 
        ///   as their parent, or <see langword="false"/> otherwise.
        /// </returns>
        private bool HasRegisteredChildren(string nodeId) {
            return _nodesById.Values.Any(x => x.Parent != null && string.Equals(x.Parent, nodeId, StringComparison.Ordinal));
        }


        /// <summary>
        /// Deletes a node and all of its descendencts from the <see cref="AssetModelManager"/> 
        /// cache and the underlying <see cref="IKeyValueStore"/>.
        /// </summary>
        /// <param name="nodeId">
        ///   The ID of the node to delete.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the delete was successful, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="AssetModelManager"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="nodeId"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> DeleteNodeAsync(string nodeId, CancellationToken cancellationToken = default) {
            ThrowOnDisposed();

            if (nodeId == null) {
                throw new ArgumentNullException(nameof(nodeId));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            var node = GetNode(nodeId);
            if (node == null) {
                return false;
            }

            return await DeleteNodeCoreAsync(node, true, true, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Recursively deletes a node and all of its descendents.
        /// </summary>
        /// <param name="node">
        ///   The node.
        /// </param>
        /// <param name="checkParent">
        ///   The <see langword="true"/>, the parent of the deleted <paramref name="node"/> will 
        ///   be checked to see if its <see cref="AssetModelNode.HasChildren"/> property needs to 
        ///   be updated following the deleting of the <paramref name="node"/>.
        /// </param>
        /// <param name="requiresLock">
        ///   <see langword="true"/> if <see cref="_writeLock"/> must be acquired before updating, 
        ///   or <see langword="false"/> if it has already been acquired.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A flag indicating if the delete was successful.
        /// </returns>
        private async ValueTask<bool> DeleteNodeCoreAsync(AssetModelNode node, bool checkParent, bool requiresLock, CancellationToken cancellationToken) {
            if (requiresLock) {
                await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            try {
                // "nodes:{id}" key contains the definition with ID {id}.
                var result = await _keyValueStore.DeleteAsync(string.Concat("nodes:", node.Id)).ConfigureAwait(false);

                if (result == KeyValueStoreOperationStatus.OK) {
                    _nodesById.TryRemove(node.Id, out _);

                    // "nodes" key contains an array of the defined node IDs.
                    await _keyValueStore.WriteAsync("nodes", _nodesById.Keys.ToArray()).ConfigureAwait(false);

                    // If the deleted node has any children, delete them as well.
                    if (node.HasChildren) {
                        foreach (var child in _nodesById.Values.Where(x => x.Parent != null && x.Parent.Equals(node.Id, StringComparison.Ordinal))) {
                            await DeleteNodeCoreAsync(child, false, false, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    // If the node has a parent, ensure that we are not removing the parent's last
                    // child.
                    if (checkParent && node.Parent != null) {
                        var parent = GetNode(node.Parent);
                        if (parent != null && parent.HasChildren) {
                            var hasChildren = HasRegisteredChildren(parent.Id);
                            if (!hasChildren) {
                                await AddOrUpdateNodeCoreAsync(new AssetModelNodeBuilder(parent).WithChildren(false).Build(), false, cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }
                }

                return result == KeyValueStoreOperationStatus.OK;
            }
            finally {
                if (requiresLock) {
                    _writeLock.Release();
                }
            }
        }


        /// <summary>
        /// Moves the specified node to a new parent node.
        /// </summary>
        /// <param name="nodeId">
        ///   The ID of the node to move.
        /// </param>
        /// <param name="parentId">
        ///   The ID of the new parent node. Specify <see langword="null"/> to make the node a 
        ///   top-level node.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the node was successfully moved, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   The <see cref="AssetModelManager"/> has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="nodeId"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask<bool> MoveNodeAsync(string nodeId, string? parentId, CancellationToken cancellationToken = default) {
            ThrowOnDisposed();

            if (nodeId == null) {
                throw new ArgumentNullException(nameof(nodeId));
            }

            await _initTask.Value.WithCancellation(cancellationToken).ConfigureAwait(false);

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                var node = GetNode(nodeId);
                if (node == null) {
                    // Node does not exist.
                    return false;
                }

                if (parentId != null && GetNode(parentId) == null) {
                    // Parent node does not exist.
                    return false;
                }

                if (string.Equals(node.Parent, parentId, StringComparison.OrdinalIgnoreCase)) {
                    // Node is already in the correct place.
                    return true;
                }

                // Update node.
                var oldParentId = node.Parent;
                await AddOrUpdateNodeCoreAsync(new AssetModelNodeBuilder(node).WithParent(parentId).Build(), false, cancellationToken).ConfigureAwait(false);

                // Update old parent node if required.
                if (oldParentId != null) {
                    var oldParent = GetNode(oldParentId);
                    if (oldParent != null) {
                        var hasChildren = HasRegisteredChildren(oldParent.Id);
                        if (!hasChildren) {
                            await AddOrUpdateNodeCoreAsync(new AssetModelNodeBuilder(oldParent).WithChildren(false).Build(), false, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                return true;
            }
            finally {
                _writeLock.Release();
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<AssetModelNode> BrowseAssetModelNodes(
            IAdapterCallContext context,
            BrowseAssetModelNodesRequest request,
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

            if (request.ParentId == null) {
                // Browse top-level nodes.
                foreach (var item in _nodesById.Values.Where(x => x.Parent == null).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).SelectPage(request)) {
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }
                    yield return item;
                }

                yield break;
            }

            // Browse starting at specified node.

            if (!_nodesById.TryGetValue(request.ParentId, out var parent) || !parent.HasChildren) {
                // Parent does not exist or no children.
                yield break;
            }

            foreach (var item in _nodesById.Values.Where(x => string.Equals(x.Parent, request.ParentId, StringComparison.Ordinal)).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).SelectPage(request)) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                yield return item;
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<AssetModelNode> GetAssetModelNodes(
            IAdapterCallContext context, 
            GetAssetModelNodesRequest request, 
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

            foreach (var item in request.Nodes) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                if (item == null || !_nodesById.TryGetValue(item, out var node)) {
                    continue;
                }

                yield return node;
            }
        }


        /// <inheritdoc/>
        public async IAsyncEnumerable<AssetModelNode> FindAssetModelNodes(
            IAdapterCallContext context, 
            FindAssetModelNodesRequest request,
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

            foreach (var item in _nodesById.Values.Where(x => string.IsNullOrEmpty(request.Name) || x.Name.Like(request.Name!)).Where(x => string.IsNullOrEmpty(request.Description) || x.Description.Like(request.Description!))) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                yield return item;
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();
            _writeLock.Dispose();
            _nodesById.Clear();
            _disposed = true;

            GC.SuppressFinalize(this);
        }
    }
}
