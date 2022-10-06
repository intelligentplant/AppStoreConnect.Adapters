using System;
using System.Threading.Tasks;
using System.Timers;

using DataCore.Adapter.AspNetCore.SignalR.Client;

using Timer = System.Timers.Timer;

namespace DataCore.Adapter.Http.Proxy {

    /// <summary>
    /// Wraps an <see cref="AdapterSignalRClient"/> so that the client can be automatically 
    /// disposed after a period where there are no active streaming calls.
    /// </summary>
    internal class SignalRClientWrapper : IDisposable {

        /// <summary>
        /// Flags if the object has been disposed
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// The SignalR API client.
        /// </summary>
        private readonly AdapterSignalRClient _client;

        /// <summary>
        /// The identifier for the client wrapper.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The SignalR API client.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///   Thw wrapper has been disposed.
        /// </exception>
        public AdapterSignalRClient Client {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return _client;
            }
        }

        /// <summary>
        /// The timer that will fire when the wrapper should check if it should dispose of the client.
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// Lock for performing client dispose checks.
        /// </summary>
        private readonly Nito.AsyncEx.AsyncLock _lock = new Nito.AsyncEx.AsyncLock();

        /// <summary>
        /// The number of streams currently active on the client.
        /// </summary>
        private int _activeStreamCount;

        /// <summary>
        /// Raised when the <see cref="SignalRClientWrapper"/> is disposed.
        /// </summary>
        public event Action<SignalRClientWrapper>? Disposed;


        /// <summary>
        /// Creates a new <see cref="SignalRClientWrapper"/> object.
        /// </summary>
        /// <param name="key">
        ///   The key for the client wrapper.
        /// </param>
        /// <param name="client">
        ///   The SignalR API client to wrap.
        /// </param>
        /// <param name="timeToLive">
        ///   The time-to-live for the client when it is idle.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        public SignalRClientWrapper(string key, AdapterSignalRClient client, TimeSpan timeToLive) {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _timer = new Timer() {
                AutoReset = false,
                Interval = timeToLive.TotalMilliseconds
            };
            _timer.Elapsed += OnTimerElapsed;
        }


        /// <summary>
        /// Handles <see cref="Timer.Elapsed"/> events.
        /// </summary>
        /// <param name="sender">
        ///   The timer.
        /// </param>
        /// <param name="args">
        ///   The event arguments.
        /// </param>
        private void OnTimerElapsed(object sender, ElapsedEventArgs args) {
            if (_disposed) {
                return;
            }

            using (_lock.Lock()) {
                if (_activeStreamCount < 1) {
                    Dispose();
                }
            }
        }


        /// <summary>
        /// Informs the wrapper that a streaming operation has started.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will increment the number of active streams and stop 
        ///   the time-to-live timer.
        /// </returns>
        internal async ValueTask StreamStartedAsync() {
            if (_disposed) {
                return;
            }

            using (await _lock.LockAsync().ConfigureAwait(false)) {
                if (_disposed) {
                    return;
                }
                ++_activeStreamCount;
                if (_timer.Enabled) {
                    _timer.Enabled = false;
                }
            }
        }


        /// <summary>
        /// Informs the wrapper that a streaming operation has ended.
        /// </summary>
        /// <returns>
        ///   A <see cref="ValueTask"/> that will decrement the number of active streams and 
        ///   enable the time-to-live timer if no more active streams remain.
        /// </returns>
        internal async ValueTask StreamCompletedAsync() {
            if (_disposed) {
                return;
            }

            using (await _lock.LockAsync().ConfigureAwait(false)) {
                if (_disposed) {
                    return;
                }
                --_activeStreamCount;
                if (_activeStreamCount < 1) {
                    _timer.Enabled = true;
                }
            }
        }


        /// <inheritdoc/>
        public void Dispose() {
            if (_disposed) {
                return;
            }

            using (_lock.Lock()) {
                _timer.Dispose();
                _client.Dispose();
                _disposed = true;
            }

            Disposed?.Invoke(this);
        }

    }
}
