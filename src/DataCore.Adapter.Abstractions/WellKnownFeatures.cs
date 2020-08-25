using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter {

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1724 // Type names should not match namespaces

    /// <summary>
    /// Defines URIs for well-known adapter features.
    /// </summary>
    public static class WellKnownFeatures {

        /// <summary>
        /// Defines URIs for well-known asset model adapter features.
        /// </summary>

        public static class AssetModel {
            /// <summary>
            /// URI for <see cref="IAssetModelBrowse"/>.
            /// </summary>
            public const string AssetModelBrowse = "asc:features/asset-model/browse/";

            /// <summary>
            /// URI for <see cref="IAssetModelSearch"/>.
            /// </summary>
            public const string AssetModelSearch = "asc:features/asset-model/search/";

        }


        /// <summary>
        /// Defines URIs for well-known diagnostics adapter features.
        /// </summary>
        public static class Diagnostics {

            /// <summary>
            /// URI for <see cref="IHealthCheck"/>.
            /// </summary>
            public const string HealthCheck = "asc:features/diagnostics/health-check/";

        }


        /// <summary>
        /// Defines URIs for well-known alarm &amp; event adapter features.
        /// </summary>
        public static class Events {

            /// <summary>
            /// URI for <see cref="IEventMessagePush"/>.
            /// </summary>
            public const string EventMessagePush = "asc:features/events/push/";

            /// <summary>
            /// URI for <see cref="IEventMessagePushWithTopics"/>.
            /// </summary>
            public const string EventMessagePushWithTopics = "asc:features/events/push/topics/";

            /// <summary>
            /// URI for <see cref="IReadEventMessagesForTimeRange"/>.
            /// </summary>
            public const string ReadEventMessagesForTimeRange = "asc:features/events/read/time/";

            /// <summary>
            /// URI for <see cref="IReadEventMessagesUsingCursor"/>.
            /// </summary>
            public const string ReadEventMessagesUsingCursor = "asc:features/events/read/cursor/";

            /// <summary>
            /// URI for <see cref="IWriteEventMessages"/>.
            /// </summary>
            public const string WriteEventMessages = "asc:features/events/write/";

        }


        /// <summary>
        /// Defines URIs for well-known real-time data adapter features.
        /// </summary>
        public static class RealTimeData {

            /// <summary>
            /// URI for <see cref="IReadTagValueAnnotations"/>.
            /// </summary>
            public const string ReadAnnotations = "asc:features/real-time-data/annotations/read/";

            /// <summary>
            /// URI for <see cref="IReadPlotTagValues"/>.
            /// </summary>
            public const string ReadPlotTagValues = "asc:features/real-time-data/values/read/plot/";

            /// <summary>
            /// URI for <see cref="IReadProcessedTagValues"/>.
            /// </summary>
            public const string ReadProcessedTagValues = "asc:features/real-time-data/values/read/processed/";

            /// <summary>
            /// URI for <see cref="IReadRawTagValues"/>.
            /// </summary>
            public const string ReadRawTagValues = "asc:features/real-time-data/values/read/raw/";

            /// <summary>
            /// URI for <see cref="IReadSnapshotTagValues"/>.
            /// </summary>
            public const string ReadSnapshotTagValues = "asc:features/real-time-data/values/read/snapshot/";

            /// <summary>
            /// URI for <see cref="IReadTagValuesAtTimes"/>.
            /// </summary>
            public const string ReadTagValuesAtTimes = "asc:features/real-time-data/values/read/at-times/";

            /// <summary>
            /// URI for <see cref="ISnapshotTagValuePush"/>.
            /// </summary>
            public const string SnapshotTagValuePush = "asc:features/real-time-data/values/push/";

            /// <summary>
            /// URI for <see cref="ITagInfo"/>.
            /// </summary>
            public const string TagInfo = "asc:features/real-time-data/tags/info/";

            /// <summary>
            /// URI for <see cref="ITagSearch"/>.
            /// </summary>
            public const string TagSearch = "asc:features/real-time-data/tags/search/";

            /// <summary>
            /// URI for <see cref="IWriteTagValueAnnotations"/>.
            /// </summary>
            public const string WriteAnnotations = "asc:features/real-time-data/annotations/write/";

            /// <summary>
            /// URI for <see cref="IWriteHistoricalTagValues"/>.
            /// </summary>
            public const string WriteHistoricalTagValues = "asc:features/real-time-data/values/write/history/";

            /// <summary>
            /// URI for <see cref="IWriteSnapshotTagValues"/>.
            /// </summary>
            public const string WriteSnapshotTagValues = "asc:features/real-time-data/values/write/snapshot/";

        }

    }

#pragma warning restore CA1034 // Nested types should not be visible
#pragma warning restore CA1724 // Type names should not match namespaces

}
