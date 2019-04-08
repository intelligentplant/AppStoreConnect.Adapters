using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// <see cref="IObserver{T}"/>-style interface, which includes information in every observer 
    /// method about which adapter the message is being received from. This is to allow the same 
    /// observer to be used to subscribe to multiple adapters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAdapterObserver<T> {

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter descriptor.
        /// </param>
        /// <param name="value">
        ///   The new value.
        /// </param>
        Task OnNext(AdapterDescriptor adapter, T value);

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter descriptor.
        /// </param>
        /// <param name="error">
        ///   The error.
        /// </param>
        Task OnError(AdapterDescriptor adapter, Exception error);

        /// <summary>
        /// Notifies the observer that the adapter has finished sending push-based notifications.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter descriptor.
        /// </param>
        Task OnCompleted(AdapterDescriptor adapter);

    }
}
