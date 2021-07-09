using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using FASTER.core;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// Default <see cref="IKeyValueStore"/> implementation.
    /// </summary>
    public class FasterKeyValueStore : IKeyValueStore, IDisposable, IAsyncDisposable {

        /// <summary>
        /// Flags if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// A <see cref="CancellationTokenSource"/> that will request cancellation when the store 
        /// is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Indicates if trace logging can be performed.
        /// </summary>
        private readonly bool _logTrace;

        /// <summary>
        /// The underlying FASTER store.
        /// </summary>
        private readonly FasterKV<SpanByte, SpanByte> _fasterKVStore;

        /// <summary>
        /// The FASTER device used to store the on-disk portion of the log.
        /// </summary>
        private readonly IDevice _logDevice;

        /// <summary>
        /// Session builder for the FASTER store.
        /// </summary>
        private readonly FasterKV<SpanByte, SpanByte>.ClientSessionBuilder<SpanByte, SpanByteAndMemory, Empty> _clientSessionBuilder;

        /// <summary>
        /// Holds pooled FASTER sessions to allow multiple threads to concurrently read or modify 
        /// the store.
        /// </summary>
        private readonly ConcurrentQueue<ClientSession<SpanByte, SpanByte, SpanByte, SpanByteAndMemory, Empty, SpanByteFunctions<Empty>>> _sessionPool = new();

        /// <summary>
        /// Serializer for store values.
        /// </summary>
        private readonly IFasterKeyValueStoreSerializer _serializer;

        /// <summary>
        /// Flags if the store should run in read-only mode.
        /// </summary>
        private readonly bool _readOnly;

        /// <summary>
        /// Flags if periodic snapshots of the entire cache should be persisted to disk.
        /// </summary>
        private readonly bool _canRecordCheckpoints;

        /// <summary>
        /// Flags if the periodic checkpoint loop should create a new checkpoint the next 
        /// time it runs.
        /// </summary>
        private int _checkpointIsRequired;

        /// <summary>
        /// The size (in bytes) that the read-only portion of the FASTER log must reach before 
        /// compaction will be performed. This field will be updated over the lifetime of the 
        /// store as the log size increases.
        /// </summary>
        private long _logCompactionThresholdBytes;

        /// <summary>
        /// Tracks the number of successive compaction checks that resulted in log compaction 
        /// being performed. This is used to identify if the <see cref="_logCompactionThresholdBytes"/> 
        /// needs to be increased in order to reduce the number of compaction operations.
        /// </summary>
        private byte _numConsecutiveLogCompactions;

        /// <summary>
        /// The maximum number of times that consecutive iterations of the <see cref="RunLogCompactionLoopAsync"/> 
        /// can compact the FASTER log before the <see cref="_logCompactionThresholdBytes"/> limit 
        /// will be increased.
        /// </summary>
        private const byte ConsecutiveCompactOperationsBeforeThresholdIncrease = 5;


        /// <summary>
        /// Creates a new <see cref="FasterKeyValueStore"/> object.
        /// </summary>
        /// <param name="options">
        ///   The options for the store.
        /// </param>
        /// <param name="logger">
        ///   The <see cref="ILogger"/> for the store.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public FasterKeyValueStore(FasterKeyValueStoreOptions options, ILogger<FasterKeyValueStore>? logger = null) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            _logger = (ILogger) logger! ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _logTrace = _logger.IsEnabled(LogLevel.Trace);

            var logSettings = CreateLogSettings(options);

            _logDevice = logSettings.LogDevice;

            var checkpointManager = options.CheckpointManagerFactory?.Invoke();
            if (checkpointManager == null) {
                _logger.LogWarning(Resources.Log_NoCheckpointManagerProvided);
            }

            _fasterKVStore = new FasterKV<SpanByte, SpanByte>(
                options.IndexBucketCount,
                logSettings,
                checkpointManager == null ? null : new CheckpointSettings() { 
                    CheckpointManager = checkpointManager
                }
            );

            _clientSessionBuilder = _fasterKVStore.For(new SpanByteFunctions<Empty>());
            _serializer = options.Serializer ?? new JsonFasterKeyValueStoreSerializer();
            _readOnly = options.ReadOnly;

            if (checkpointManager != null) {
                try {
                    _fasterKVStore.Recover();
                }
                catch (FasterException e) {
                    // Exception will be thrown if there is not a checkpoint to recover from.
                    _logger.LogWarning(e, Resources.Log_ErrorWhileRecoveringCheckpoint);
                }

                _canRecordCheckpoints = !_readOnly;

                if (_canRecordCheckpoints && options.CheckpointInterval.HasValue && options.CheckpointInterval.Value > TimeSpan.Zero) {
                    // Start periodic checkpoint task.
                    _ = RunCheckpointCreationLoopAsync(options.CheckpointInterval.Value, _disposedTokenSource.Token);
                }
            }

            if (!_readOnly && options.CompactionInterval.HasValue && options.CompactionInterval.Value > TimeSpan.Zero) {
                _logCompactionThresholdBytes = options.CompactionThresholdBytes <= 0
                    ? (long) Math.Pow(2, options.MemorySizeBits) * 2
                    : options.CompactionThresholdBytes;
                _ = RunLogCompactionLoopAsync(options.CompactionInterval.Value, _disposedTokenSource.Token);
            }
        }


        /// <summary>
        /// Creates an <see cref="ICheckpointManager"/> that will store checkpoints using local 
        /// storage.
        /// </summary>
        /// <param name="path">
        ///   The directory to store checkpoints in. If a relative path is specified, it will be 
        ///   made absolute relative to <see cref="AppContext.BaseDirectory"/>.
        /// </param>
        /// <param name="removeOutdated">
        ///   When <see langword="true"/>, the <see cref="ICheckpointManager"/> will remove older 
        ///   checkpoints as newer checkpoints are recorded.
        /// </param>
        /// <returns>
        ///   A new <see cref="ICheckpointManager"/> instance.
        /// </returns>
        /// <seealso cref="FasterKeyValueStoreOptions.CheckpointManagerFactory"/>
        public static ICheckpointManager CreateLocalStorageCheckpointManager(string path, bool removeOutdated = true) {
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }

            if (!Path.IsPathRooted(path)) {
                path = Path.Combine(AppContext.BaseDirectory, path);
            }

            var di = new DirectoryInfo(path);
            // Ensure that path exists.
            di.Create();

            return new DeviceLogCommitCheckpointManager(
                new LocalStorageNamedDeviceFactory(),
                new DefaultCheckpointNamingScheme(di.FullName),
                removeOutdated: removeOutdated
            );
        }


        /// <summary>
        /// Creates the <see cref="LogSettings"/> for the underlying FASTER log.
        /// </summary>
        /// <param name="options">
        ///   The options for the store.
        /// </param>
        /// <returns>
        ///   A new <see cref="LogSettings"/> object.
        /// </returns>
        private static LogSettings CreateLogSettings(FasterKeyValueStoreOptions options) {
            IDevice CreateDefaultDevice() {
                return Devices.CreateLogDevice(
                    Path.Combine(Path.GetTempPath(), "FASTER", "hlog.log"),
                    preallocateFile: false,
                    // This is a temporary path so we want to delete all files when the device is
                    // disposed.
                    deleteOnClose: true
                );
            }

            var logSettings = new LogSettings {
                LogDevice = options.LogDeviceFactory?.Invoke() ?? CreateDefaultDevice(),
                PageSizeBits = options.PageSizeBits,
                MemorySizeBits = options.MemorySizeBits,
                SegmentSizeBits = options.SegmentSizeBits
            };

            return logSettings;
        }


        /// <summary>
        /// Long-running task that will periodically take a full snapshot checkpoint of the FASTER 
        /// log and persist it using the configured <see cref="ICheckpointManager"/>.
        /// </summary>
        /// <param name="interval">
        ///   The snapshot interval.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will create a snapshot of the FASTER log at the specified 
        ///   <paramref name="interval"/>.
        /// </returns>
        private async Task RunCheckpointCreationLoopAsync(TimeSpan interval, CancellationToken cancellationToken) {
            while (!_disposed && !cancellationToken.IsCancellationRequested) {
                try {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    await TakeFullCheckpointAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    _logger.LogError(e, Resources.Log_ErrorWhileCreatingCheckpoint);
                }
            }
        }


        /// <summary>
        /// Takes a full snapshot checkpoint.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return a flag indicating if a 
        ///   checkpoint was created.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   Checkpoint management is disabled.
        /// </exception>
        /// <remarks>
        ///   A checkpoint will only be created if items have been added to, updated in, or 
        ///   removed from the store since the last checkpoint was taken. 
        /// </remarks>
        public async ValueTask<bool> TakeFullCheckpointAsync(CancellationToken cancellationToken = default) {
            if (!_canRecordCheckpoints) {
                throw new InvalidOperationException(Resources.Error_CheckpointsAreDisabled);
            }

            if (Interlocked.CompareExchange(ref _checkpointIsRequired, 0, 1) != 1) {
                // No checkpoint pending.
                return false;
            }

            try {
                var (success, token) = await _fasterKVStore.TakeFullCheckpointAsync(CheckpointType.Snapshot, cancellationToken).ConfigureAwait(false);
                return success;
            }
            catch {
                Interlocked.Exchange(ref _checkpointIsRequired, 1);
                throw;
            }
        }


        /// <summary>
        /// Takes a full snapshot checkpoint.
        /// </summary>
        /// <remarks>
        ///   This method is intended to be called from <see cref="Dispose(bool)"/> and nowhere else!
        /// </remarks>
        private void TakeFullCheckpoint() {
            if (Interlocked.CompareExchange(ref _checkpointIsRequired, 0, 1) != 1) {
                // No checkpoint pending.
                return;
            }
            try {
                _fasterKVStore.TakeFullCheckpoint(out _, CheckpointType.Snapshot);
                // We need to wait for the checkpoint to complete and there is not a
                // synchronous way of doing this. We avoid doing this if TakeFullCheckpointAsync
                // is called instead!
                _fasterKVStore.CompleteCheckpointAsync().GetAwaiter().GetResult();
            }
            catch {
                Interlocked.Exchange(ref _checkpointIsRequired, 1);
                throw;
            }
        }


        /// <summary>
        /// Long-running task that will periodically perform compaction of the FASTER log if 
        /// required.
        /// </summary>
        /// <param name="interval">
        ///   The compaction interval.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will compact the FASTER log at the specified <paramref name="interval"/> 
        ///   if it is larger than the current compaction threshold.
        /// </returns>
        private async Task RunLogCompactionLoopAsync(TimeSpan interval, CancellationToken cancellationToken) {
            var logAccessor = _fasterKVStore.Log;
            while (!_disposed && !cancellationToken.IsCancellationRequested) {
                try {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);

                    // (oldest entries here) BeginAddress <= HeadAddress (where the in-memory region begins) <= SafeReadOnlyAddress (entries between here and tail updated in-place) < TailAddress (entries added here)
                    var safeReadOnlyRegionByteSize = logAccessor.SafeReadOnlyAddress - logAccessor.BeginAddress;
                    if (safeReadOnlyRegionByteSize < _logCompactionThresholdBytes) {
                        if (_logTrace) {
                            _logger.LogTrace(string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.Log_SkippingLogCompaction, 
                                safeReadOnlyRegionByteSize, 
                                _logCompactionThresholdBytes
                            ));
                        }
                        _numConsecutiveLogCompactions = 0;
                        continue;
                    }

                    var compactUntilAddress = (long) (logAccessor.BeginAddress + 0.2 * (logAccessor.SafeReadOnlyAddress - logAccessor.BeginAddress));
                    var session = GetPooledSession();
                    try {
                        session.Compact(compactUntilAddress, true);
                        // Log has been modified; we need a new checkpoint to be created.
                        Interlocked.Exchange(ref _checkpointIsRequired, 1);
                    }
                    finally {
                        ReturnPooledSession(session);
                    }

                    _numConsecutiveLogCompactions++;

                    if (_logTrace) {
                        _logger.LogTrace(string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.Log_LogCompacted,
                            safeReadOnlyRegionByteSize,
                            logAccessor.SafeReadOnlyAddress - logAccessor.BeginAddress,
                            _numConsecutiveLogCompactions
                        ));
                    }

                    if (_numConsecutiveLogCompactions >= ConsecutiveCompactOperationsBeforeThresholdIncrease) {
                        _logCompactionThresholdBytes *= 2;
                        if (_logTrace) {
                            _logger.LogTrace(string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.Log_LogCompactionThresholdIncreased, 
                                _logCompactionThresholdBytes / 2, 
                                _logCompactionThresholdBytes
                            ));
                        }
                        _numConsecutiveLogCompactions = 0;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    _logger.LogError(e, Resources.Log_CompactionError);
                }
            }
        }


        /// <summary>
        /// Gets a pooled FASTER session, or creates a new session if no pooled sessions are 
        /// available.
        /// </summary>
        /// <returns>
        ///   A session object that can be used to query or modify the FASTER log.
        /// </returns>
        private ClientSession<SpanByte, SpanByte, SpanByte, SpanByteAndMemory, Empty, SpanByteFunctions<Empty>> GetPooledSession() {
            if (_sessionPool.TryDequeue(out ClientSession<SpanByte, SpanByte, SpanByte, SpanByteAndMemory, Empty, SpanByteFunctions<Empty>>? result)) {
                return result;
            }

            return CreateSession();
        }


        /// <summary>
        /// Creates a new FASTER session object.
        /// </summary>
        /// <returns>
        ///   A new FASTER session object.
        /// </returns>
        private ClientSession<SpanByte, SpanByte, SpanByte, SpanByteAndMemory, Empty, SpanByteFunctions<Empty>> CreateSession() {
            return _clientSessionBuilder.NewSession<SpanByteFunctions<Empty>>();
        }


        /// <summary>
        /// Returns a FASTER session object to the pool for use by another thread.
        /// </summary>
        /// <param name="session">
        ///   The FASTER session object.
        /// </param>
        private void ReturnPooledSession(ClientSession<SpanByte, SpanByte, SpanByte, SpanByteAndMemory, Empty, SpanByteFunctions<Empty>> session) {
            _sessionPool.Enqueue(session);
        }


        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
        /// </summary>
        private void ThrowIfDisposed() {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }


        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the store is running in read-only mode.
        /// </summary>
        private void ThrowIfReadOnly() {
            if (_readOnly) {
                throw new InvalidOperationException(Resources.Error_StoreIsReadOnly);
            }
        }


        /// <inheritdoc/>
        public async ValueTask<KeyValueStoreOperationStatus> WriteAsync<TValue>(byte[] key, TValue? value) {
            ThrowIfDisposed();
            ThrowIfReadOnly();

            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var session = GetPooledSession();
            try {
                var keySpanByte = SpanByte.FromFixedSpan(key);
                var valueSpanByte = SpanByte.FromFixedSpan(_serializer.Serialize(value));

                var result = await session.UpsertAsync(ref keySpanByte, ref valueSpanByte).ConfigureAwait(false);

                while (result.Status == Status.PENDING) {
                    result = await result.CompleteAsync().ConfigureAwait(false);
                }

                // Mark the cache as dirty.
                Interlocked.Exchange(ref _checkpointIsRequired, 1);

                return ToKeyValueOperationStatus(result.Status);
            }
            finally {
                ReturnPooledSession(session);
            }
        }


        /// <inheritdoc/>
        public async ValueTask<KeyValueStoreReadResult<TValue>> ReadAsync<TValue>(byte[] key) {
            ThrowIfDisposed();
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            Status status;
            SpanByteAndMemory spanByteAndMemory;

            var session = GetPooledSession();
            try {
                var keySpanByte = SpanByte.FromFixedSpan(key);
                SpanByte valueSpanByte = default;

                var result = await session.ReadAsync(ref keySpanByte, ref valueSpanByte).ConfigureAwait(false);

                (status, spanByteAndMemory) = result.Complete();
            }
            finally {
                ReturnPooledSession(session);
            }

            using (spanByteAndMemory.Memory) {
                return new KeyValueStoreReadResult<TValue>(
                    ToKeyValueOperationStatus(status),
                    status == Status.OK
                        ? _serializer.Deserialize<TValue>(spanByteAndMemory.Memory.Memory.Slice(0, spanByteAndMemory.Length).Span)
                        : default
                    );
            }
        }


        /// <inheritdoc/>
        public async ValueTask<KeyValueStoreOperationStatus> DeleteAsync(byte[] key) {
            ThrowIfDisposed();
            ThrowIfReadOnly();

            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var session = GetPooledSession();
            try {
                var keySpanByte = SpanByte.FromFixedSpan(key);

                var result = await session.DeleteAsync(ref keySpanByte).ConfigureAwait(false);

                while (result.Status == Status.PENDING) {
                    result = await result.CompleteAsync().ConfigureAwait(false);
                }

                if (result.Status != Status.NOTFOUND) {
                    // Mark the cache as dirty.
                    Interlocked.Exchange(ref _checkpointIsRequired, 1);
                }

                return ToKeyValueOperationStatus(result.Status);
            }
            finally {
                ReturnPooledSession(session);
            }
        }


        /// <inheritdoc/>
        public IEnumerable<byte[]> GetKeys() {
            var session = GetPooledSession();
            try {
                using (var iterator = session.Iterate()) {
                    while (iterator.GetNext(out var recordInfo)) {
                        yield return iterator.GetKey().AsReadOnlySpan().ToArray();
                    }
                }
            }
            finally {
                ReturnPooledSession(session);
            }
        }


        /// <summary>
        /// Converts a FASTER <see cref="Status"/> to an <see cref="OperationStatus"/>.
        /// </summary>
        /// <param name="status">
        ///   The FASTER <see cref="Status"/>.
        /// </param>
        /// <returns>
        ///   The equivalent <see cref="OperationStatus"/>.
        /// </returns>
        private static KeyValueStoreOperationStatus ToKeyValueOperationStatus(Status status) {
            switch (status) {
                case Status.ERROR:
                    return KeyValueStoreOperationStatus.Error;
                case Status.NOTFOUND:
                    return KeyValueStoreOperationStatus.NotFound;
                case Status.OK:
                default:
                    return KeyValueStoreOperationStatus.OK;
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
        }


        /// <summary>
        /// Disposes of managed resources.
        /// </summary>
        private void DisposeCommon() {
            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();

            // Dispose of all sessions.
            while (_sessionPool.TryDequeue(out var session)) {
                session.Dispose();
            }

            // Dispose of log.
            _fasterKVStore.Dispose();

            // Dispose of underlying log device.
            _logDevice.Dispose();
        }


        /// <summary>
        /// Disposes of resources.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true"/> if the object is being disposed, or <see langword="false"/> 
        ///   if it is being finalized.
        /// </param>
        protected virtual void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                if (_canRecordCheckpoints) {
                    TakeFullCheckpoint();
                }
                DisposeCommon();
            }

            _disposed = true;
        }


        /// <summary>
        /// Asynchronously disposes of resources.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will asynchronously dispose of resources.
        /// </returns>
        protected virtual async ValueTask DisposeAsyncCore() {
            if (_canRecordCheckpoints) {
                await TakeFullCheckpointAsync(default).ConfigureAwait(false);
            }
            DisposeCommon();
        }

    }
}
