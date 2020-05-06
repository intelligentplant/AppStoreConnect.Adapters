using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Monitors adapter options and notifies when they are changed.
    /// </summary>
    /// <typeparam name="TAdapterOptions">
    ///   The adapter options type.
    /// </typeparam>
    public interface IAdapterOptionsMonitor<TAdapterOptions> where TAdapterOptions : AdapterOptions {

        /// <summary>
        /// The current adapter options.
        /// </summary>
        TAdapterOptions CurrentValue { get; }

        /// <summary>
        /// Registers a listener to be notified when the adapter options are changed.
        /// </summary>
        /// <param name="listener">
        ///   The listener.
        /// </param>
        /// <returns>
        ///   An object that can be disposed to release the listener registration.
        /// </returns>
        IDisposable OnChange(Action<TAdapterOptions> listener);

    }
}
