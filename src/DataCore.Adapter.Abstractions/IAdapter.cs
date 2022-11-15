using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes an App Store Connect adapter.
    /// </summary>
    /// <remarks>
    ///   Implementers should inherit from <see cref="AdapterCore"/> or a another derived type 
    ///   rather than implementing <see cref="IAdapter"/> directly. 
    /// </remarks>
    /// <seealso cref="AdapterCore"/>
    public interface IAdapter : IBackgroundTaskServiceProvider, IDisposable, IAsyncDisposable {

        /// <summary>
        /// Gets the adapter descriptor.
        /// </summary>
        AdapterDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the adapter type descriptor.
        /// </summary>
        AdapterTypeDescriptor TypeDescriptor { get; }

        /// <summary>
        /// Gets the feature collection for the adapter.
        /// </summary>
        IAdapterFeaturesCollection Features { get; }

        /// <summary>
        /// Gets additional properties associated with the adapter.
        /// </summary>
        IEnumerable<AdapterProperty> Properties { get; }

        /// <summary>
        /// Gets a flag indicating if the adapter is enabled.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets a flag indicating if the adapter has been started.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Raised when the adapter is started.
        /// </summary>
        event Func<IAdapter, Task>? Started;

        /// <summary>
        /// Raised when the adapter is stopped.
        /// </summary>
        event Func<IAdapter, Task>? Stopped;

        /// <summary>
        /// Starts the adapter.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that represents the start operation.
        /// </returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Shuts down the adapter.
        /// </summary>
        /// <param name="cancellationToken">
        ///   The cancellation token for the operation.
        /// </param>
        /// <returns>
        ///   A <see cref="Task"/> that represents the stop operation.
        /// </returns>
        /// <remarks>
        ///   The <see cref="StopAsync"/> method is intended to allow the same adapter to be 
        ///   started and stopped multiple times. Therefore, only resources that are created 
        ///   when <see cref="StartAsync"/> is called should be disposed when <see cref="StopAsync"/> 
        ///   is called. The <see cref="IDisposable.Dispose"/> and <see cref="IAsyncDisposable.DisposeAsync"/> 
        ///   methods should be used to dispose of all resources, including those created by 
        ///   calls to <see cref="StartAsync"/>.
        /// </remarks>
        Task StopAsync(CancellationToken cancellationToken);

    }
}
