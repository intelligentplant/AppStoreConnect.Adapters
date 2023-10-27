using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Services;

using FASTER.core;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// Default <see cref="IKeyValueStore"/> implementation.
    /// </summary>
    public partial class FasterKeyValueStore : RawKeyValueStore<FasterKeyValueStoreOptions>, IDisposable, IAsyncDisposable {

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
        private int _fullCheckpointIsRequired;

        /// <summary>
        /// Lock used to ensure that incremental and full checkpoints cannot be ongoing at the 
        /// same time.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncLock _checkpointLock = new Nito.AsyncEx.AsyncLock();

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

        /// <inheritdoc/>
        protected override bool AllowRawWrites => Options.EnableRawWrites;


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
        public FasterKeyValueStore(FasterKeyValueStoreOptions options, ILogger<FasterKeyValueStore>? logger = null) : base(options, logger) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            var logSettings = CreateLogSettings(options);

            _logDevice = logSettings.LogDevice;

            var checkpointManager = options.CheckpointManagerFactory?.Invoke();
            if (checkpointManager == null) {
                LogCheckpointManagerIsDisabled(Logger);
            }

            _fasterKVStore = new FasterKV<SpanByte, SpanByte>(
                options.IndexBucketCount,
                logSettings,
                checkpointManager == null ? null : new CheckpointSettings() { 
                    CheckpointManager = checkpointManager
                }
            );

            _clientSessionBuilder = _fasterKVStore.For(new SpanByteFunctions<Empty>());
            _readOnly = options.ReadOnly;

            if (checkpointManager != null) {
                try {
                    _fasterKVStore.Recover();
                }
                catch (FasterException e) {
                    // Exception will be thrown if there is not a checkpoint to recover from.
                    LogErrorWhileRecoveringCheckpoint(Logger, e);
                }

                _canRecordCheckpoints = !_readOnly;

                if (_canRecordCheckpoints && options.CheckpointInterval.HasValue && options.CheckpointInterval.Value > TimeSpan.Zero) {
                    // Start periodic full checkpoint task.
                    _ = RunFullCheckpointLoopAsync(options.CheckpointInterval.Value, _disposedTokenSource.Token);
                }

                if (_canRecordCheckpoints && options.IncrementalCheckpointInterval.HasValue && options.IncrementalCheckpointInterval.Value > TimeSpan.Zero) {
                    // Start periodic incremental checkpoint task.
                    _ = RunIncrementalCheckpointLoopAsync(options.IncrementalCheckpointInterval.Value, _disposedTokenSource.Token);
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
        private async Task RunFullCheckpointLoopAsync(TimeSpan interval, CancellationToken cancellationToken) {
            while (!_disposed && !cancellationToken.IsCancellationRequested) {
                try {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    await TakeFullCheckpointAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    LogErrorWhileCreatingFullCheckpoint(Logger, e);
                }
            }
        }


        /// <summary>
        /// Long-running task that will periodically take an incremental snapshot checkpoint of the FASTER 
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
        private async Task RunIncrementalCheckpointLoopAsync(TimeSpan interval, CancellationToken cancellationToken) {
            while (!_disposed && !cancellationToken.IsCancellationRequested) {
                try {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    await TakeIncrementalCheckpointAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    LogErrorWhileCreatingIncrementalCheckpoint(Logger, e);
                }
            }
        }


        /// <summary>
        /// Takes a full snapshot checkpoint of both the FASTER index and the log.
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

            if (Interlocked.CompareExchange(ref _fullCheckpointIsRequired, 0, 1) != 1) {
                // No checkpoint pending.
                return false;
            }

            using (await _checkpointLock.LockAsync(cancellationToken).ConfigureAwait(false)) {
                try {
                    var (success, token) = await _fasterKVStore.TakeFullCheckpointAsync(CheckpointType.Snapshot, cancellationToken).ConfigureAwait(false);
                    return success;
                }
                catch {
                    Interlocked.Exchange(ref _fullCheckpointIsRequired, 1);
                    throw;
                }
            }
        }


        /// <summary>
        /// Takes an incremental checkpoint of the FASTER log.
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
        public async ValueTask<bool> TakeIncrementalCheckpointAsync(CancellationToken cancellationToken = default) {
            if (!_canRecordCheckpoints) {
                throw new InvalidOperationException(Resources.Error_CheckpointsAreDisabled);
            }

            using (await _checkpointLock.LockAsync(cancellationToken).ConfigureAwait(false)) {
                var(success, token) = await _fasterKVStore.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot, tryIncremental: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                return success;
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
                        LogSkippingCompaction(Logger, safeReadOnlyRegionByteSize, _logCompactionThresholdBytes);
                        _numConsecutiveLogCompactions = 0;
                        continue;
                    }

                    var compactUntilAddress = (long) (logAccessor.BeginAddress + 0.2 * (logAccessor.SafeReadOnlyAddress - logAccessor.BeginAddress));
                    var session = GetPooledSession();
                    try {
                        session.Compact(compactUntilAddress, CompactionType.Scan);
                        // Log has been modified; we need a new checkpoint to be created.
                        Interlocked.Exchange(ref _fullCheckpointIsRequired, 1);
                    }
                    finally {
                        ReturnPooledSession(session);
                    }

                    _numConsecutiveLogCompactions++;
                    LogCompactionCompleted(Logger, safeReadOnlyRegionByteSize, logAccessor.SafeReadOnlyAddress - logAccessor.BeginAddress, _numConsecutiveLogCompactions);

                    if (_numConsecutiveLogCompactions >= ConsecutiveCompactOperationsBeforeThresholdIncrease) {
                        _logCompactionThresholdBytes *= 2;
                        LogCompactionThresholdIncreased(Logger, _logCompactionThresholdBytes / 2, _logCompactionThresholdBytes);
                        _numConsecutiveLogCompactions = 0;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    LogCompactionError(Logger, e);
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
        protected override async ValueTask WriteAsync<T>(KVKey key, T value) {
            await WriteCoreAsync(key, await SerializeToBytesAsync(value).ConfigureAwait(false)).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async ValueTask WriteRawAsync(KVKey key, byte[] value) {
            await WriteCoreAsync(key, value).ConfigureAwait(false);
        }


        /// <summary>
        /// Writes raw byte data to the store.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <param name="value">
        ///   The raw byte data.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will process the operation.
        /// </returns>
        private async ValueTask WriteCoreAsync(KVKey key, byte[] value) {
            ThrowIfDisposed();
            ThrowIfReadOnly();

            var session = GetPooledSession();
            try {
                var keySpanByte = SpanByte.FromFixedSpan((byte[]) key);
                var valueSpanByte = SpanByte.FromFixedSpan(value);

                var result = await session.UpsertAsync(ref keySpanByte, ref valueSpanByte).ConfigureAwait(false);

                while (result.Status.IsPending) {
                    result = await result.CompleteAsync().ConfigureAwait(false);
                }

                // Mark the cache as dirty.
                Interlocked.Exchange(ref _fullCheckpointIsRequired, 1);
            }
            finally {
                ReturnPooledSession(session);
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<T?> ReadAsync<T>(KVKey key) where T: default {
            var data = await ReadCoreAsync(key).ConfigureAwait(false);
            if (data == null) {
                return default;
            }

            return await DeserializeFromBytesAsync<T>(data).ConfigureAwait(false);
        }


        /// <inheritdoc/>
        protected override async ValueTask<byte[]?> ReadRawAsync(KVKey key) {
            return await ReadCoreAsync(key).ConfigureAwait(false);
        }


        /// <summary>
        /// Reads raw bytes from the store.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <returns>
        ///   The raw bytes, or <see langword="null"/> if the key does not exist.
        /// </returns>
        private async ValueTask<byte[]?> ReadCoreAsync(KVKey key) {
            ThrowIfDisposed();

            Status status;
            SpanByteAndMemory spanByteAndMemory;

            var session = GetPooledSession();
            try {
                var keySpanByte = SpanByte.FromFixedSpan((byte[]) key);
                SpanByte valueSpanByte = default;

                var result = await session.ReadAsync(ref keySpanByte, ref valueSpanByte).ConfigureAwait(false);

                (status, spanByteAndMemory) = result.Complete();
            }
            finally {
                ReturnPooledSession(session);
            }

            if (!status.Found) {
                return null;
            }

            using (spanByteAndMemory.Memory) {
                return spanByteAndMemory.Memory.Memory.Slice(0, spanByteAndMemory.Length).ToArray();
            }
        }


        /// <inheritdoc/>
        protected override async ValueTask<bool> DeleteAsync(KVKey key) {
            ThrowIfDisposed();
            ThrowIfReadOnly();

            var session = GetPooledSession();
            try {
                var keySpanByte = SpanByte.FromFixedSpan((byte[]) key);

                var result = await session.DeleteAsync(ref keySpanByte).ConfigureAwait(false);

                while (result.Status.IsPending) {
                    result = await result.CompleteAsync().ConfigureAwait(false);
                }

                if (result.Status.Found) {
                    // Mark the cache as dirty.
                    Interlocked.Exchange(ref _fullCheckpointIsRequired, 1);
                }

                return result.Status.IsCompletedSuccessfully;
            }
            finally {
                ReturnPooledSession(session);
            }
        }


        /// <inheritdoc/>
        protected override async IAsyncEnumerable<KVKey> GetKeysAsync(KVKey? prefix) {
            await foreach (var item in GetRecordsCoreAsync(prefix, false).ConfigureAwait(false)) {
                yield return item.Key;
            }
        }


        /// <summary>
        /// Gets all keys and metadata in the FASTER KV store with the specified prefix.
        /// </summary>
        /// <param name="prefix">
        ///   The prefix to filter by.
        /// </param>
        /// <returns>
        ///   A sequence of <see cref="FasterRecord"/> objects.
        /// </returns>
        /// <remarks>
        ///   Note that calling <see cref="GetRecordsAsync"/> will iterate over every record in 
        ///   the FASTER store. Use with caution!
        /// </remarks>
        public async IAsyncEnumerable<FasterRecord> GetRecordsAsync(KVKey? prefix = null) {
            await foreach (var item in GetRecordsCoreAsync(prefix, true).ConfigureAwait(false)) {
                yield return item;
            }
        }


        /// <summary>
        /// Gets all keys and metadata in the FASTER KV store with the specified prefix.
        /// </summary>
        /// <param name="prefix">
        ///   The prefix to filter by.
        /// </param>
        /// <param name="includeValues">
        ///   Specifies if record values should be included in the <see cref="FasterRecord"/> instances.
        /// </param>
        /// <returns>
        ///   A sequence of <see cref="FasterRecord"/> objects.
        /// </returns>
        private async IAsyncEnumerable<FasterRecord> GetRecordsCoreAsync(KVKey? prefix, bool includeValues) {
            var session = GetPooledSession();
            try {
                var funcs = new ScanIteratorFunctions(includeValues);

                session.Iterate(ref funcs);

                while (await funcs.Reader.WaitToReadAsync().ConfigureAwait(false)) {
                    while (funcs.Reader.TryRead(out var item)) {
                        if (prefix == null || prefix.Value.Length == 0 || StartsWithPrefix(prefix.Value, item.Key)) {
                            yield return item;
                        }
                    }
                }
            }
            finally {
                ReturnPooledSession(session);
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
                    using (var @lock = new ManualResetEventSlim()) {
                        _ = Task.Run(async () => { 
                            try {
                                await TakeFullCheckpointAsync().ConfigureAwait(false);
                            }
                            catch (Exception e) {
                                LogErrorWhileCreatingFullCheckpoint(Logger, e);
                            }
                            finally {
                                @lock.Set();
                            }
                        });
                        @lock.Wait();
                    }
                    
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


        [LoggerMessage(100, LogLevel.Information, "Checkpoint management is disabled; backup and restore of data will not be performed.")]
        static partial void LogCheckpointManagerIsDisabled(ILogger logger);

        [LoggerMessage(101, LogLevel.Warning, "Error while recovering the FASTER log from the latest checkpoint. This message can be ignored the first time the FASTER store is used as there will be no checkpoint available to recover from.")]
        static partial void LogErrorWhileRecoveringCheckpoint(ILogger logger, Exception e);

        [LoggerMessage(102, LogLevel.Error, "Error while creating an incremental recovery checkpoint for the FASTER log.")]
        static partial void LogErrorWhileCreatingIncrementalCheckpoint(ILogger logger, Exception e);

        [LoggerMessage(103, LogLevel.Error, "Error while creating a full recovery checkpoint for the FASTER log.")]
        static partial void LogErrorWhileCreatingFullCheckpoint(ILogger logger, Exception e);

        [LoggerMessage(104, LogLevel.Trace, "Skipping FASTER log compaction. Safe read-only region size: {safeReadOnlyRegionByteSize} bytes. Threshold: {logCompactionThresholdBytes} bytes.")]
        static partial void LogSkippingCompaction(ILogger logger, long safeReadOnlyRegionByteSize, long logCompactionThresholdBytes);

        [LoggerMessage(105, LogLevel.Trace, "FASTER log compaction completed. Safe read-only region size before: {sizeBeforeBytes} bytes. Size after: {sizeAfterBytes} bytes. Consecutive compactions: {consecutiveCompactions}")]
        static partial void LogCompactionCompleted(ILogger logger, long sizeBeforeBytes, long sizeAfterBytes, byte consecutiveCompactions);

        [LoggerMessage(106, LogLevel.Trace, "Increasing FASTER log compaction threshold. Previous threshold: {oldThresholdBytes} bytes. New threshold: {newThresholdBytes} bytes.")]
        static partial void LogCompactionThresholdIncreased(ILogger logger, long oldThresholdBytes, long newThresholdBytes);

        [LoggerMessage(107, LogLevel.Error, "Error while performing FASTER log compaction.")]
        static partial void LogCompactionError(ILogger logger, Exception e);


        /// <summary>
        /// <see cref="IScanIteratorFunctions{Key, Value}"/> implementation that allows us to use 
        /// FASTER's "push" key iteration: https://microsoft.github.io/FASTER/docs/fasterkv-basics/#key-iteration
        /// </summary>
        private struct ScanIteratorFunctions : IScanIteratorFunctions<SpanByte, SpanByte> {

            /// <summary>
            /// The channel to publish keys to as they are iterated over.
            /// </summary>
            private readonly Channel<FasterRecord> _channel;

            /// <summary>
            /// Specifies if the iterator should include record values.
            /// </summary>
            private readonly bool _includeValues;

            /// <summary>
            /// The channel reader.
            /// </summary>
            public ChannelReader<FasterRecord> Reader => _channel?.Reader!;

            internal ScanIteratorFunctions(bool includeValues) {
                _includeValues = includeValues;
                _channel = Channel.CreateUnbounded<FasterRecord>(new UnboundedChannelOptions() {
                    AllowSynchronousContinuations = false,
                    SingleReader = true,
                    SingleWriter = true
                });
            }

            
            /// <inheritdoc/>
            public bool OnStart(long beginAddress, long endAddress) => true;


            /// <inheritdoc/>
            public bool SingleReader(ref SpanByte key, ref SpanByte value, RecordMetadata recordMetadata, long numberOfRecords) {
                return _channel.Writer.TryWrite(new FasterRecord(key.AsReadOnlySpan().ToArray(), recordMetadata, false, _includeValues ? new ReadOnlyMemory<byte>(value.AsReadOnlySpan().ToArray()) : default));
            }


            /// <inheritdoc/>
            public bool ConcurrentReader(ref SpanByte key, ref SpanByte value, RecordMetadata recordMetadata, long numberOfRecords) {
                return _channel.Writer.TryWrite(new FasterRecord(key.AsReadOnlySpan().ToArray(), recordMetadata, true, _includeValues ? new ReadOnlyMemory<byte>(value.AsReadOnlySpan().ToArray()) : default));
            }


            /// <inheritdoc/>
            public void OnStop(bool completed, long numberOfRecords) {
                _channel.Writer.TryComplete();
            }


            /// <inheritdoc/>
            public void OnException(Exception exception, long numberOfRecords) {
                _channel.Writer.TryComplete(exception);
            }

        }


        /// <summary>
        /// A record in the FASTER store.
        /// </summary>
        public readonly struct FasterRecord {

            /// <summary>
            /// The key.
            /// </summary>
            public KVKey Key { get; }

            /// <summary>
            /// The metadata for the record.
            /// </summary>
            public RecordMetadata Metadata { get; }

            /// <summary>
            /// Specifies if the record is located in the mutable portion of the FASTER log.
            /// </summary>
            public bool Mutable { get; }

            /// <summary>
            /// The value for the record.
            /// </summary>
            public ReadOnlyMemory<byte> Value { get; }


            /// <summary>
            /// Creates a new <see cref="FasterRecord"/> instance
            /// </summary>
            /// <param name="key">
            ///   The key.
            /// </param>
            /// <param name="metadata">
            ///   The metadata for the record.
            /// </param>
            /// <param name="mutable">
            ///   Specifies if the record is located in the mutable portion of the FASTER log.
            /// </param>
            /// <param name="value">
            ///   The record value.
            /// </param>
            internal FasterRecord(KVKey key, RecordMetadata metadata, bool mutable, ReadOnlyMemory<byte> value) {
                Key = key;
                Metadata = metadata;
                Mutable = mutable;
                Value = value;
            }

        }

    }
}
