using System;
using System.Diagnostics.Tracing;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// <see cref="EventSource"/> that adapters can use to emit lifecycle events.
    /// </summary>
    [EventSource(
        Name = ActivitySourceExtensions.DiagnosticSourceName,
        LocalizationResources = "DataCore.Adapter.Diagnostics.AdapterEventSourceResources"
    )]
    public partial class AdapterEventSource : EventSource {

        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterStarted"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that started.
        /// </param>
        [Event(EventIds.AdapterStarted, Level = EventLevel.Informational)]
        public void AdapterStarted(string adapterId) {
            WriteEvent(EventIds.AdapterStarted, adapterId);
        }


        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterStopped"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that stopped.
        /// </param>
        [Event(EventIds.AdapterStopped, Level = EventLevel.Informational)]
        public void AdapterStopped(string adapterId) {
            WriteEvent(EventIds.AdapterStopped, adapterId);
        }


        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterUpdated"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that was updated.
        /// </param>
        [Event(EventIds.AdapterUpdated, Level = EventLevel.Informational)]
        public void AdapterUpdated(string adapterId) {
            WriteEvent(EventIds.AdapterUpdated, adapterId);
        }


        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterDisposed"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that was disposed.
        /// </param>
        [Event(EventIds.AdapterDisposed, Level = EventLevel.Informational)]
        public void AdapterDisposed(string adapterId) {
            WriteEvent(EventIds.AdapterDisposed, adapterId);
        }


        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterOperationStarted"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that the operation is being run on.
        /// </param>
        /// <param name="operationId">
        ///   An identifier for the operation.
        /// </param>
        [Event(EventIds.AdapterOperationStarted, Level = EventLevel.Informational, Opcode = EventOpcode.Start)]
        public void AdapterOperationStarted(string adapterId, string operationId) {
            WriteEvent(EventIds.AdapterOperationStarted, adapterId, operationId);
        }


        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterOperationCompleted"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that the operation was being run on.
        /// </param>
        /// <param name="operationId">
        ///   An identifier for the operation.
        /// </param>
        /// <param name="elapsed">
        ///   The elapsed time for the operation, in milliseconds.
        /// </param>
        [Event(EventIds.AdapterOperationCompleted, Level = EventLevel.Informational, Opcode = EventOpcode.Stop)]
        public void AdapterOperationCompleted(string adapterId, string operationId, double elapsed) {
            WriteEvent(EventIds.AdapterOperationCompleted, adapterId, operationId, elapsed);
        }


        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterOperationFaulted"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that the operation was being run on.
        /// </param>
        /// <param name="operationId">
        ///   An identifier for the operation.
        /// </param>
        /// <param name="elapsed">
        ///   The elapsed time for the operation, in milliseconds.
        /// </param>
        /// <param name="error">
        ///   The error message associated with the fault.
        /// </param>
        [Event(EventIds.AdapterOperationFaulted, Level = EventLevel.Error, Opcode = EventOpcode.Stop)]
        public void AdapterOperationFaulted(string adapterId, string operationId, double elapsed, string error) {
            WriteEvent(EventIds.AdapterOperationFaulted, adapterId, operationId, elapsed, error);
        }


        /// <summary>
        /// Event IDs that can be emitted by the <see cref="AdapterEventSource"/>.
        /// </summary>
        public static class EventIds {

            /// <summary>
            /// An adapter was started.
            /// </summary>
            public const int AdapterStarted = 1;

            /// <summary>
            /// An adapter stopped.
            /// </summary>
            public const int AdapterStopped = 2;

            /// <summary>
            /// An adapter was updated.
            /// </summary>
            public const int AdapterUpdated = 3;

            /// <summary>
            /// An adapter was disposed.
            /// </summary>
            public const int AdapterDisposed = 4;

            /// <summary>
            /// An operation on an adapter was started.
            /// </summary>
            public const int AdapterOperationStarted = 5;

            /// <summary>
            /// An operation on an adapter was completed successfully.
            /// </summary>
            public const int AdapterOperationCompleted = 6;

            /// <summary>
            /// An operation on an adapter was completed unsuccessfully.
            /// </summary>
            public const int AdapterOperationFaulted = 7;

        }

    }
}
