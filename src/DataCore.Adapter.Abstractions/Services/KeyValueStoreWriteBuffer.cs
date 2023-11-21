using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Services {

    /// <summary>
    /// A helper class that caches write and delete operations for an <see cref="IRawKeyValueStore"/> 
    /// and flushes changes on a periodic basis, or when maximum key or size limits are exceeded.
    /// </summary>
    /// <remarks>
    ///   Use <see cref="KeyValueStoreWriteBuffer"/> with <see cref="IRawKeyValueStore"/> 
    ///   implementations that will benefit from writing or deleting keys in batches.
    /// </remarks>
    public sealed partial class KeyValueStoreWriteBuffer : IDisposable {

        /// <summary>
        /// Specifies if the object has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The options for the buffer.
        /// </summary>
        private readonly KeyValueStoreWriteBufferOptions _options;

        /// <summary>
        /// The callback to invoke when the buffer is flushed.
        /// </summary>
        private readonly Func<IEnumerable<KeyValuePair<KVKey, byte[]?>>, Task> _callback;

        /// <summary>
        /// The pending writes to the store.
        /// </summary>
        /// <remarks>
        ///   An entry with a <see langword="null"/> value indicates that the entry will be deleted.
        /// </remarks>
        private readonly Dictionary<KVKey, byte[]?> _pendingChanges = new Dictionary<KVKey, byte[]?>();

        /// <summary>
        /// The size of all pending writes to the store.
        /// </summary>
        private long _pendingWritesSize;

        /// <summary>
        /// Used to signal when a flush has completed.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncManualResetEvent _flushEvent = new Nito.AsyncEx.AsyncManualResetEvent(false);

        /// <summary>
        /// A cancellation token source that is cancelled when the object is disposed.
        /// </summary>
        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();

        /// <summary>
        /// A lock for accessing <see cref="_pendingChanges"/> and <see cref="_pendingWritesSize"/>.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncReaderWriterLock _lock = new Nito.AsyncEx.AsyncReaderWriterLock();

        /// <summary>
        /// Logging.
        /// </summary>
        private readonly ILogger<KeyValueStoreWriteBuffer> _logger;


        /// <summary>
        /// Creates a new <see cref="KeyValueStoreWriteBuffer"/> instance.
        /// </summary>
        /// <param name="options">
        ///   The buffer options.
        /// </param>
        /// <param name="callback">
        ///   The callback to invoke when the buffer is flushed.
        /// </param>
        /// <param name="logger">
        ///   The logger to use for the buffer.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public KeyValueStoreWriteBuffer(
            KeyValueStoreWriteBufferOptions options,
            Func<IEnumerable<KeyValuePair<KVKey, byte[]?>>, Task> callback,
            ILogger<KeyValueStoreWriteBuffer>? logger = null
        ) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<KeyValueStoreWriteBuffer>.Instance;

            _ = Task.Run(() => RunPeriodicFlushAsync(_disposedTokenSource.Token));
        }


        /// <summary>
        /// Reads a serialized value from the buffer.
        /// </summary>
        /// <param name="key">
        ///   The key.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask{TResult}"/> that will return the value from the buffer, or 
        ///   <see langword="null"/>.
        /// </returns>
        public async ValueTask<ReadResult> ReadAsync(KVKey key, CancellationToken cancellationToken = default) {
            if (_disposed) {
                return default;
            }

            using (await _lock.ReaderLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (_disposed) {
                    return default;
                }

                if (_pendingChanges.TryGetValue(key, out byte[]? value)) {
                    return new ReadResult(true, value);
                }

                return default;
            }
        }


        /// <summary>
        /// Adds a pending write to the buffer.
        /// </summary>
        /// <param name="key">
        ///   The key to write to.
        /// </param>
        /// <param name="value">
        ///   The serialized value.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will add the operation to the buffer.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public async ValueTask WriteAsync(KVKey key, byte[] value, CancellationToken cancellationToken = default) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            if (_disposed) {
                return;
            }

            var immediateFlush = false;
            var immediateFlushReason = "";

            using (await _lock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (_disposed) {
                    return;
                }

                if (_pendingChanges.TryGetValue(key, out var bytes) && bytes != null) {
                    _pendingWritesSize -= bytes.Length;
                }
                _pendingChanges[key] = value;
                _pendingWritesSize += value.Length;

                if (_options.KeyLimit > 0 && _pendingChanges.Count >= _options.KeyLimit) {
                    immediateFlush = true;
                    immediateFlushReason = "key limit exceeded";
                }
                else if (_options.SizeLimit > 0 && _pendingWritesSize >= _options.SizeLimit) {
                    immediateFlush = true;
                    immediateFlushReason = "size limit exceeded";
                }
            }

            if (immediateFlush) {
                await FlushCoreAsync(immediateFlushReason).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Adds a pending delete to the buffer.
        /// </summary>
        /// <param name="key">
        ///   The key to delete.
        /// </param>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will add the operation to the buffer.
        /// </returns>
        public async ValueTask DeleteAsync(KVKey key, CancellationToken cancellationToken = default) {
            if (_disposed) {
                return;
            }

            var immediateFlush = false;

            using (await _lock.WriterLockAsync(cancellationToken).ConfigureAwait(false)) {
                if (_disposed) {
                    return;
                }

                if (_pendingChanges.TryGetValue(key, out var bytes) && bytes != null) {
                    _pendingWritesSize -= bytes.Length;
                }
                _pendingChanges[key] = null;

                if (_options.KeyLimit > 0 && _pendingChanges.Count >= _options.KeyLimit) {
                    immediateFlush = true;
                }
            }

            if (immediateFlush) {
                await FlushCoreAsync("key limit exceeded").ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Runs the periodic flush task.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that will run the periodic flush task.
        /// </returns>
        private async Task RunPeriodicFlushAsync(CancellationToken cancellationToken) {
            var interval = _options.FlushInterval;
            if (interval <= TimeSpan.Zero) {
                interval = TimeSpan.FromSeconds(5);
            }

            LogFlushEnabled(interval);

            while (!cancellationToken.IsCancellationRequested) {
                try {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    await FlushCoreAsync("periodic flush").ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception e) {
                    LogFlushError(e);
                }
            }

            // Final flush once cancellation has been requested.
            try {
                await FlushCoreAsync("periodic flush cancelled").ConfigureAwait(false);
            }
            catch (Exception e) {
                LogFlushError(e);
            }
        }


        /// <summary>
        /// Flushes pending changes to the store.
        /// </summary>
        /// <param name="reason">
        ///   The reason for the flush.
        /// </param>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will flush pending changes to the store.
        /// </returns>
        private async ValueTask FlushCoreAsync(string reason) {
            try {
                using (await _lock.WriterLockAsync().ConfigureAwait(false)) {
                    if (_pendingChanges.Count == 0) {
                        return;
                    }

                    LogFlushStarted(_pendingChanges.Count, reason);

                    await _callback(_pendingChanges).ConfigureAwait(false);
                    _pendingChanges.Clear();
                }
            }
            finally {
                _flushEvent.Set();
                _flushEvent.Reset();
            }
        }


        /// <summary>
        /// Manually flushes pending changes to the store.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will flush pending changes to the store.
        /// </returns>
        public async ValueTask FlushAsync() {
            if (_disposed) {
                return;
            }

            await FlushCoreAsync("manual flush requested").ConfigureAwait(false);
        }


        /// <summary>
        /// Waits for the next flush to complete.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token to use for the operation.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        ///   If the store is configured to flush pending writes to the database immediately, 
        ///   <see cref="WaitForNextFlushAsync"/> will return immediately.
        /// </remarks>
        public async ValueTask WaitForNextFlushAsync(CancellationToken cancellationToken = default) {
            if (_disposed) {
                return;
            }

            Task t;
            using (await _lock.ReaderLockAsync(cancellationToken)) {
                if (_disposed) {
                    return;
                }
                t = _flushEvent.WaitAsync(cancellationToken);
            }
            await t.ConfigureAwait(false);
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            _disposedTokenSource.Cancel();
            _disposedTokenSource.Dispose();

            _disposed = true;
        }


        [LoggerMessage(1, LogLevel.Information, "Changes will be flushed to the store at an interval of {flushInterval}.")]
        partial void LogFlushEnabled(TimeSpan flushInterval);

        [LoggerMessage(2, LogLevel.Trace, "Flushing {count} pending changes to the store. Reason: {reason}")]
        partial void LogFlushStarted(int count, string reason);

        [LoggerMessage(3, LogLevel.Error, "Error while flushing pending changes to the store.")]
        partial void LogFlushError(Exception e);


        /// <summary>
        /// The result of a call to <see cref="ReadAsync"/>
        /// </summary>
        public readonly struct ReadResult {

            /// <summary>
            /// Specifies if the key was found in the <see cref="KeyValueStoreWriteBuffer"/>
            /// </summary>
            public bool Found { get; }

            /// <summary>
            /// The value, if found.
            /// </summary>
            public byte[]? Value { get; }


            /// <summary>
            /// Creates a new <see cref="ReadResult"/> instance.
            /// </summary>
            /// <param name="found">
            ///   <see langword="true"/> if the key was found in the <see cref="KeyValueStoreWriteBuffer"/>; 
            ///   otherwise, <see langword="false"/>.
            /// </param>
            /// <param name="value">
            ///   The value, if found.
            /// </param>
            public ReadResult(bool found, byte[]? value) {
                Found = found;
                Value = value;
            }

        }

    }
}
