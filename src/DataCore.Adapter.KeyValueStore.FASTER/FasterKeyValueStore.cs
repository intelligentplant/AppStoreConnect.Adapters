using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Services;

using FASTER.core;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.KeyValueStore.FASTER {

    /// <summary>
    /// <see cref="IKeyValueStore"/> implementation that uses <a href="https://microsoft.github.io/FASTER/">Microsoft FASTER</a> as its backing store.
    /// </summary>
    public sealed partial class FasterKeyValueStore : RawKeyValueStore<FasterKeyValueStoreOptions>, IAsyncDisposable {

        /// <summary>
        /// Active instances of <see cref="FasterKeyValueStore"/>. These are tracked to allow 
        /// metrics to be reported for each instance.
        /// </summary>
        private static readonly List<FasterKeyValueStore> s_instances = new List<FasterKeyValueStore>();

        /// <summary>
        /// Lock for accessing <see cref="s_instances"/>.
        /// </summary>
        private static readonly Nito.AsyncEx.AsyncReaderWriterLock s_instancesLock = new Nito.AsyncEx.AsyncReaderWriterLock();

        /// <summary>
        /// Creates a metric tag that identifies the <see cref="FasterKeyValueStore"/> instance 
        /// that a measurement was observed on.
        /// </summary>
        /// <param name="instance">
        ///   The <see cref="FasterKeyValueStore"/> instance.
        /// </param>
        /// <returns>
        ///   A new <see cref="KeyValuePair{TKey, TValue}"/> that can be added to the metric 
        ///   measurement.
        /// </returns>
        private static KeyValuePair<string, object?> CreateInstanceIdTag(FasterKeyValueStore instance) => new KeyValuePair<string, object?>("data_core.instance_id", instance._instanceName);

        /// <summary>
        /// Instrument for observing the total in-memory footprint for <see cref="FasterKeyValueStore"/> 
        /// instances.
        /// </summary>
        private static readonly ObservableGauge<long> s_totalSizeGauge = Telemetry.Meter.CreateObservableGauge(
            "KeyValueStore.FASTER.Size.Total",
            () => {
                using (s_instancesLock.ReaderLock()) {
                    return s_instances.Select(x => new Measurement<long>(x._sizeTracker.GetTotalSizeBytes(), CreateInstanceIdTag(x))).ToArray();
                }
            },
            "By",
            "Total size of the FASTER index, in-memory log and read cache.");

        /// <summary>
        /// Instrument for observing the size of the in-memory index for <see cref="FasterKeyValueStore"/> 
        /// instances.
        /// </summary>
        private static readonly ObservableGauge<long> s_indexSizeGauge = Telemetry.Meter.CreateObservableGauge(
            "KeyValueStore.FASTER.Size.Index",
            () => {
                using (s_instancesLock.ReaderLock()) {
                    return s_instances.Select(x => new Measurement<long>(x._sizeTracker.GetIndexSizeBytes(), CreateInstanceIdTag(x))).ToArray();
                }
            },
            "By",
            "Size of the FASTER index.");

        /// <summary>
        /// Instrument for observing the size of the in-memory log for <see cref="FasterKeyValueStore"/> 
        /// instances.
        /// </summary>
        private static readonly ObservableGauge<long> s_logSize = Telemetry.Meter.CreateObservableGauge(
            "KeyValueStore.FASTER.Size.Log",
            () => {
                using (s_instancesLock.ReaderLock()) {
                    return s_instances.Select(x => new Measurement<long>(x._sizeTracker.GetLogSizeBytes(), CreateInstanceIdTag(x))).ToArray();
                }
            },
            "By",
            "Size of the in-memory portion of the FASTER log.");

        /// <summary>
        /// Instrument for observing the size of the reach cache for <see cref="FasterKeyValueStore"/> 
        /// instances.
        /// </summary>
        private static readonly ObservableGauge<long> s_readCacheSize = Telemetry.Meter.CreateObservableGauge(
            "KeyValueStore.FASTER.Size.ReadCache",
            () => {
                using (s_instancesLock.ReaderLock()) {
                    return s_instances.Select(x => new Measurement<long>(x._sizeTracker.GetReadCacheSizeBytes(), CreateInstanceIdTag(x))).ToArray();
                }
            },
            "By",
            "Size of the FASTER read cache.");

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
        /// The name of the store instance. This name is used in metrics.
        /// </summary>
        private readonly string _instanceName;

        /// <summary>
        /// The underlying FASTER store.
        /// </summary>
        private readonly FasterKV<SpanByte, SpanByte> _fasterKVStore;

        /// <summary>
        /// The size tracker for the im-memory portion and read cache for the FASTER store.
        /// </summary>
        private readonly CacheSizeTracker _sizeTracker;

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

            _instanceName = string.IsNullOrWhiteSpace(options.Name)
                ? "$default"
                : options.Name!;

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
            _sizeTracker = new CacheSizeTracker(_fasterKVStore);

            _clientSessionBuilder = _fasterKVStore.For(new SizeTrackingSpanByteFunctions(_sizeTracker));
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

            using (s_instancesLock.WriterLock()) {
                s_instances.Add(this);
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
        private async ValueTask WriteCoreAsync(byte[] key, byte[] value) {
            ThrowIfDisposed();
            ThrowIfReadOnly();

            var session = GetPooledSession();

            // We need to pin the key and value bytes for the duration of the upsert, as required
            // when using SpanByte: https://github.com/microsoft/FASTER/pull/349

            var keyHandle = GCHandle.Alloc(key, GCHandleType.Pinned);
            var valueHandle = GCHandle.Alloc(value, GCHandleType.Pinned);

            try {
                var keySpanByte = new SpanByte(key.Length, keyHandle.AddrOfPinnedObject());
                var valueSpanByte = new SpanByte(value.Length, valueHandle.AddrOfPinnedObject());

                var result = await session.UpsertAsync(ref keySpanByte, ref valueSpanByte).ConfigureAwait(false);

                while (result.Status.IsPending) {
                    result = await result.CompleteAsync().ConfigureAwait(false);
                }

                // Mark the cache as dirty.
                Interlocked.Exchange(ref _fullCheckpointIsRequired, 1);
            }
            finally {
                keyHandle.Free();
                valueHandle.Free();

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
        private async ValueTask<byte[]?> ReadCoreAsync(byte[] key) {
            ThrowIfDisposed();

            Status status;
            SpanByteAndMemory spanByteAndMemory;

            var session = GetPooledSession();
            var keyHandle = GCHandle.Alloc(key, GCHandleType.Pinned);

            try {
                var keySpanByte = new SpanByte(key.Length, keyHandle.AddrOfPinnedObject());
                SpanByte valueSpanByte = default;

                var result = await session.ReadAsync(ref keySpanByte, ref valueSpanByte).ConfigureAwait(false);

                (status, spanByteAndMemory) = result.Complete();
            }
            finally {
                keyHandle.Free();
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
        protected override async ValueTask<bool> ExistsAsync(KVKey key) {
            return await ExistsCoreAsync(key).ConfigureAwait(false);
        }


        /// <summary>
        /// Tests if a key exists in the store.
        /// </summary>
        /// <param name="key">
        ///   The key to test.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the key exists; otherwise, <see langword="false"/>.
        /// </returns>
        private async ValueTask<bool> ExistsCoreAsync(byte[] key) {
            ThrowIfDisposed();

            Status status;

            var session = GetPooledSession();
            var keyHandle = GCHandle.Alloc(key, GCHandleType.Pinned);

            try {
                var keySpanByte = new SpanByte(key.Length, keyHandle.AddrOfPinnedObject());
                SpanByte valueSpanByte = default;

                var result = await session.ReadAsync(ref keySpanByte, ref valueSpanByte).ConfigureAwait(false);

                (status, _) = result.Complete();
            }
            finally {
                keyHandle.Free();
                ReturnPooledSession(session);
            }

            return status.Found;
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
        public async ValueTask DisposeAsync() {
            if (_disposed) {
                return;
            }

            using (await s_instancesLock.WriterLockAsync().ConfigureAwait(false)) {
                s_instances.Remove(this);
            }

            _disposedTokenSource.Cancel();

            // Record final checkpoint.
            if (_canRecordCheckpoints) {
                try {
                    await TakeFullCheckpointAsync(default).ConfigureAwait(false);
                }
                catch (Exception e) {
                    LogErrorWhileCreatingFullCheckpoint(Logger, e);
                }
            }

            // Dispose of all sessions.
            while (_sessionPool.TryDequeue(out var session)) {
                session.Dispose();
            }

            // Dispose of log.
            _fasterKVStore.Dispose();

            // Dispose of underlying log device.
            _logDevice.Dispose();

            _disposedTokenSource.Dispose();

            _disposed = true;
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

    }
}
