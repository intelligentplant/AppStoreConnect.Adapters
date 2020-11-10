using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter {

#pragma warning disable CA1034 // Nested types should not be visible
#pragma warning disable CA1724 // Type names should not match namespaces

    /// <summary>
    /// Defines URIs for well-known adapter features.
    /// </summary>
    public static class WellKnownFeatures {

        /// <summary>
        /// Cached URI for <see cref="Extensions.ExtensionFeatureBasePath"/>.
        /// </summary>
        internal static Uri ExtensionFeatureBasePath { get; } = new Uri(Extensions.ExtensionFeatureBasePath);

        /// <summary>
        /// Holds cached versions of all well-known features as URIs.
        /// </summary>
        internal static IReadOnlyDictionary<string, Uri> UriCache;

        /// <summary>
        /// Lookup from standard feature URI string to interface definition.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, Type> s_standardFeatureTypeLookup = new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>() { 
            [AssetModel.AssetModelBrowse] = typeof(IAssetModelBrowse),
            [AssetModel.AssetModelSearch] = typeof(IAssetModelSearch),
            [Diagnostics.ConfigurationChanges] = typeof(IConfigurationChanges),
            [Diagnostics.HealthCheck] = typeof(IHealthCheck),
            [Events.EventMessagePush] = typeof(IEventMessagePush),
            [Events.EventMessagePushWithTopics] = typeof(IEventMessagePushWithTopics),
            [Events.ReadEventMessagesForTimeRange] = typeof(IReadEventMessagesForTimeRange),
            [Events.ReadEventMessagesUsingCursor] = typeof(IReadEventMessagesUsingCursor),
            [Events.WriteEventMessages] = typeof(IWriteEventMessages),
            [RealTimeData.ReadAnnotations] = typeof(IReadTagValueAnnotations),
            [RealTimeData.ReadPlotTagValues] = typeof(IReadPlotTagValues),
            [RealTimeData.ReadProcessedTagValues] = typeof(IReadProcessedTagValues),
            [RealTimeData.ReadRawTagValues] = typeof(IReadRawTagValues),
            [RealTimeData.ReadSnapshotTagValues] = typeof(IReadSnapshotTagValues),
            [RealTimeData.ReadTagValuesAtTimes] = typeof(IReadTagValuesAtTimes),
            [RealTimeData.SnapshotTagValuePush] = typeof(ISnapshotTagValuePush),
            [RealTimeData.TagInfo] = typeof(ITagInfo),
            [RealTimeData.TagSearch] = typeof(ITagSearch),
            [RealTimeData.WriteAnnotations] = typeof(IWriteTagValueAnnotations),
            [RealTimeData.WriteHistoricalTagValues] = typeof(IWriteHistoricalTagValues),
            [RealTimeData.WriteSnapshotTagValues] = typeof(IWriteSnapshotTagValues)
        });



        /// <summary>
        /// Initialises <see cref="UriCache"/>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Complex initialisation")]
        static WellKnownFeatures() {
            var keys = s_standardFeatureTypeLookup.Keys.Concat(new[] { 
                Extensions.ExtensionFeatureBasePath
            });

            UriCache = new ReadOnlyDictionary<string, Uri>(keys.ToDictionary(k => k, k => k.CreateUriWithTrailingSlash()));
        }



        /// <summary>
        /// Gets a feature <see cref="Uri"/> from the cache, or creates a new URI object.
        /// </summary>
        /// <param name="uriString">
        ///   The URI string.
        /// </param>
        /// <returns>
        ///   The equivalent <see cref="Uri"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="uriString"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="uriString"/> is not an absolute URI.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "URI parsing")]
        public static Uri GetOrCreateFeatureUri(string uriString) {
            if (uriString == null) {
                throw new ArgumentNullException(nameof(uriString));
            }

            if (UriCache.TryGetValue(uriString, out var uri)) {
                return uri;
            }

            if (!Uri.TryCreate(uriString, UriKind.Absolute, out uri)) {
                throw new ArgumentException(SharedResources.Error_AbsoluteUriRequired, nameof(uriString));
            }

            return uri.EnsurePathHasTrailingSlash();
        }


        /// <summary>
        /// Tries to get the descriptor for the specified feature URI.
        /// </summary>
        /// <param name="featureUri">
        ///   The feature URI.
        /// </param>
        /// <param name="descriptor">
        ///   The feature descriptor.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="featureUri"/> was resolved to a standard 
        ///   feature, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryGetFeatureDescriptor(Uri featureUri, out FeatureDescriptor? descriptor) {
            descriptor = null;

            if (featureUri == null) {
                return false;
            }

            return TryGetFeatureDescriptor(featureUri.ToString(), out descriptor);
        }


        /// <summary>
        /// Tries to get the descriptor for the specified feature URI.
        /// </summary>
        /// <param name="featureUri">
        ///   The feature URI.
        /// </param>
        /// <param name="descriptor">
        ///   The feature descriptor.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="featureUri"/> was resolved to a standard 
        ///   feature, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryGetFeatureDescriptor(string featureUri, out FeatureDescriptor? descriptor) {
            descriptor = null;

            if (string.IsNullOrWhiteSpace(featureUri)) {
                return false;
            }

            if (!s_standardFeatureTypeLookup.TryGetValue(featureUri, out var type)) {
                return false;
            }

            descriptor = type.CreateFeatureDescriptor();
            return true;
        }


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
            /// URI for <see cref="IConfigurationChanges"/>.
            /// </summary>
            public const string ConfigurationChanges = "asc:features/diagnostics/configuration-changes/";

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


        /// <summary>
        /// Defines URIs related to extension features.
        /// </summary>
        public static class Extensions {

            /// <summary>
            /// The root URI for all extension features (i.e. features extension <see cref="IAdapterExtensionFeature"/>).
            /// </summary>
            public const string ExtensionFeatureBasePath = "asc:extensions/";

        }

    }

#pragma warning restore CA1034 // Nested types should not be visible
#pragma warning restore CA1724 // Type names should not match namespaces

}
