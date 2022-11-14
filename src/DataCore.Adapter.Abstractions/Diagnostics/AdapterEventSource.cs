using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// <see cref="EventSource"/> that adapters can use to emit lifecycle events.
    /// </summary>
    [EventSource(
        Name = Telemetry.DiagnosticSourceName,
        LocalizationResources = "DataCore.Adapter.Diagnostics.AdapterEventSourceResources"
    )]
    public partial class AdapterEventSource : EventSource {


        private static readonly Counter<long> s_operationsStartedCounter = Telemetry.Meter.CreateCounter<long>(
            "operations_started",
            "{operations}",
            "Adapter operations that have been started."
        );

        private static readonly Counter<long> s_operationsCompletedCounter = Telemetry.Meter.CreateCounter<long>(
            "operations_completed",
            "{operations}",
            "Adapter operations that have successfully completed."
        );

        private static readonly Counter<long> s_operationsFaultedCounter = Telemetry.Meter.CreateCounter<long>(
            "operations_faulted",
            "{operations}",
            "Adapter operations that have faulted."
        );

        private static readonly Histogram<double> s_operationTime = Telemetry.Meter.CreateHistogram<double>(
            "operation_time",
            "s",
            "Time taken to perform an adapter operation."
        );

        private static readonly Counter<long> s_streamItemsOutCounter = Telemetry.Meter.CreateCounter<long>(
            "stream_items_out",
            "{items}",
            "Items emitted by server streaming adapter operations."
        );

        private static readonly Counter<long> s_streamItemsInCounter = Telemetry.Meter.CreateCounter<long>(
            "stream_items_in",
            "{items}",
            "Items received by client streaming adapter operations."
        );


        /// <summary>
        /// Singleton <see cref="AdapterEventSource"/> instance.
        /// </summary>
        public static AdapterEventSource Log { get; } = new AdapterEventSource();


        /// <summary>
        /// Creates a new <see cref="AdapterEventSource"/> instance.
        /// </summary>
        internal AdapterEventSource() { }


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
            s_operationsStartedCounter.Add(1, new KeyValuePair<string, object?>("adapter_id", adapterId), new KeyValuePair<string, object?>("operation", operationId));
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
            var tags = new[] {
                new KeyValuePair<string, object?>("adapter_id", adapterId), 
                new KeyValuePair<string, object?>("operation", operationId)
            };
            s_operationsCompletedCounter.Add(1, tags);
            s_operationTime.Record(elapsed / 1000, tags);
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
            var tags = new[] {
                new KeyValuePair<string, object?>("adapter_id", adapterId),
                new KeyValuePair<string, object?>("operation", operationId)
            };
            s_operationsFaultedCounter.Add(1, tags);
            s_operationTime.Record(elapsed / 1000, tags);
        }


        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterStreamItemOut"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that has emitted the item.
        /// </param>
        /// <param name="operationId">
        ///   The identifier for the operation that has emitted the item.
        /// </param>
        [Event(EventIds.AdapterStreamItemOut, Level = EventLevel.Informational, Opcode = EventOpcode.Info)]
        public void AdapterStreamItemOut(string adapterId, string operationId) {
            WriteEvent(EventIds.AdapterStreamItemOut, adapterId, operationId);
            s_streamItemsOutCounter.Add(1, new KeyValuePair<string, object?>("adapter_id", adapterId), new KeyValuePair<string, object?>("operation", operationId));
        }


        /// <summary>
        /// Writes an event with ID <see cref="EventIds.AdapterStreamItemIn"/>.
        /// </summary>
        /// <param name="adapterId">
        ///   The ID of the adapter that has emitted the item.
        /// </param>
        /// <param name="operationId">
        ///   The identifier for the operation that has emitted the item.
        /// </param>
        [Event(EventIds.AdapterStreamItemIn, Level = EventLevel.Informational, Opcode = EventOpcode.Info)]
        public void AdapterStreamItemIn(string adapterId, string operationId) {
            WriteEvent(EventIds.AdapterStreamItemIn, adapterId, operationId);
            s_streamItemsInCounter.Add(1, new KeyValuePair<string, object?>("adapter_id", adapterId), new KeyValuePair<string, object?>("operation", operationId));
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

            /// <summary>
            /// An item (tag, asset model node, tag value, event) was emitted from the adapter to an asynchronous 
            /// stream.
            /// </summary>
            public const int AdapterStreamItemOut = 8;

            /// <summary>
            /// An item (tag value, event) was written to the adapter via an asynchronous stream.
            /// </summary>
            public const int AdapterStreamItemIn = 9;

        }

    }
}
