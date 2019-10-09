using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter {

    /// <summary>
    /// Monitors adapter options and notifies when they are changed.
    /// </summary>
    /// <typeparam name="TAdapterOptions">
    ///   The adapter options type.
    /// </typeparam>
    public class AdapterOptionsMonitor<TAdapterOptions> : IAdapterOptionsMonitor<TAdapterOptions> where TAdapterOptions : AdapterOptions {

        /// <summary>
        /// The listeners to be notified when the <see cref="CurrentValue"/> changes.
        /// </summary>
        private readonly List<ListenerRegistration> _listeners = new List<ListenerRegistration>();

        private TAdapterOptions _currentValue;

        /// <summary>
        /// The current adapter options.
        /// </summary>
        public TAdapterOptions CurrentValue { 
            get { return _currentValue; }
            protected set {
                _currentValue = value;
                Notify(_currentValue);
            }
        }


        /// <summary>
        /// Creates a new <see cref="AdapterOptionsMonitor{TOptions}"/> object.
        /// </summary>
        /// <param name="options">
        ///   The initial adapter options.
        /// </param>
        public AdapterOptionsMonitor(TAdapterOptions options) {
            CurrentValue = options;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterOptionsMonitor{TOptions}"/> object.
        /// </summary>
        protected AdapterOptionsMonitor() { }


        /// <summary>
        /// Notifies listeners about a change.
        /// </summary>
        /// <param name="options">
        ///   The updated adapter options.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        private void Notify(TAdapterOptions options) {
            lock (_listeners) {
                _listeners.ForEach(l => l.Notify(options));
            }
        }


        /// <summary>
        /// Registers a listener to be notified when the <see cref="CurrentValue"/> changes.
        /// </summary>
        /// <param name="listener">
        ///   The listener delegate that will receive the updated adapter options.
        /// </param>
        /// <returns>
        ///   An opject that will unregister the listener when it is disposed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="listener"/> is <see langword="null"/>.
        /// </exception>
        public IDisposable OnChange(Action<TAdapterOptions> listener) {
            var result = new ListenerRegistration(this, listener);

            lock (_listeners) {
                _listeners.Add(result);
            }

            return result;
        }


        /// <summary>
        /// Describes a listener registration on an <see cref="AdapterOptionsMonitor{TOptions}"/>.
        /// </summary>
        private class ListenerRegistration : IDisposable {

            /// <summary>
            /// Flags if the object has been disposed.
            /// </summary>
            private bool _isDisposed;

            /// <summary>
            /// The options monitor that created the registration.
            /// </summary>
            private readonly AdapterOptionsMonitor<TAdapterOptions> _monitor;

            /// <summary>
            /// The listener callback.
            /// </summary>
            private readonly Action<TAdapterOptions> _listener;


            /// <summary>
            /// Creates a new <see cref="ListenerRegistration"/> object.
            /// </summary>
            /// <param name="monitor">
            ///   The options monitor.
            /// </param>
            /// <param name="listener">
            ///   The listener callback.
            /// </param>
            /// <exception cref="ArgumentNullException">
            ///   <paramref name="monitor"/> is <see langword="null"/>.
            /// </exception>
            /// <exception cref="ArgumentNullException">
            ///   <paramref name="listener"/> is <see langword="null"/>.
            /// </exception>
            internal ListenerRegistration(AdapterOptionsMonitor<TAdapterOptions> monitor, Action<TAdapterOptions> listener) {
                _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
                _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            }


            /// <summary>
            /// Notifies the listener about an adapter options change.
            /// </summary>
            /// <param name="options">
            ///   The updated adapter options.
            /// </param>
            internal void Notify(TAdapterOptions options) {
                if (_isDisposed) {
                    return;
                }
                _listener.Invoke(options);
            }


            /// <summary>
            /// Releases the registration.
            /// </summary>
            public void Dispose() {
                if (_isDisposed) {
                    return;
                }

                _isDisposed = true;
                lock (_monitor._listeners) {
                    _monitor._listeners.Remove(this);
                }
            }

        }
    }
}
