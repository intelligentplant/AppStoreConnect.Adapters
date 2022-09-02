using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using DataCore.Adapter.AssetModel;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Events;
using DataCore.Adapter.Extensions;
using DataCore.Adapter.RealTimeData;
using DataCore.Adapter.Tags;

namespace DataCore.Adapter {

#pragma warning disable CA1724 // Type names should not match namespaces

    /// <summary>
    /// Defines URIs for well-known adapter features.
    /// </summary>
    public static class WellKnownFeatures {

        /// <summary>
        /// Cached URI for <see cref="Extensions.BaseUri"/>.
        /// </summary>
        internal static Uri ExtensionFeatureBasePath { get; } = new Uri(Extensions.BaseUri);

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
            [Extensions.CustomFunctions] = typeof(ICustomFunctions),
            [RealTimeData.ReadAnnotations] = typeof(IReadTagValueAnnotations),
            [RealTimeData.ReadPlotTagValues] = typeof(IReadPlotTagValues),
            [RealTimeData.ReadProcessedTagValues] = typeof(IReadProcessedTagValues),
            [RealTimeData.ReadRawTagValues] = typeof(IReadRawTagValues),
            [RealTimeData.ReadSnapshotTagValues] = typeof(IReadSnapshotTagValues),
            [RealTimeData.ReadTagValuesAtTimes] = typeof(IReadTagValuesAtTimes),
            [RealTimeData.SnapshotTagValuePush] = typeof(ISnapshotTagValuePush),
            [RealTimeData.WriteAnnotations] = typeof(IWriteTagValueAnnotations),
            [RealTimeData.WriteHistoricalTagValues] = typeof(IWriteHistoricalTagValues),
            [RealTimeData.WriteSnapshotTagValues] = typeof(IWriteSnapshotTagValues),
            [Tags.TagInfo] = typeof(ITagInfo),
            [Tags.TagSearch] = typeof(ITagSearch),
        });



        /// <summary>
        /// Initialises <see cref="UriCache"/>
        /// </summary>
        static WellKnownFeatures() {
            var keys = s_standardFeatureTypeLookup.Keys.Concat(new[] { 
                Extensions.BaseUri
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
            /// Base URI for asset model adapter features.
            /// </summary>
            public const string BaseUri = "asc:features/asset-model/";

            /// <summary>
            /// URI for <see cref="IAssetModelBrowse"/>.
            /// </summary>
            public const string AssetModelBrowse = BaseUri + "browse/";

            /// <summary>
            /// URI for <see cref="IAssetModelSearch"/>.
            /// </summary>
            public const string AssetModelSearch = BaseUri + "search/";

        }


        /// <summary>
        /// Defines URIs for well-known diagnostics adapter features.
        /// </summary>
        public static class Diagnostics {

            /// <summary>
            /// Base URI for diagnostics adapter features.
            /// </summary>
            public const string BaseUri = "asc:features/diagnostics/";

            /// <summary>
            /// URI for <see cref="IConfigurationChanges"/>.
            /// </summary>
            public const string ConfigurationChanges = BaseUri + "configuration-changes/";

            /// <summary>
            /// URI for <see cref="IHealthCheck"/>.
            /// </summary>
            public const string HealthCheck = BaseUri + "health-check/";

        }


        /// <summary>
        /// Defines URIs for well-known alarm &amp; event adapter features.
        /// </summary>
        public static class Events {

            /// <summary>
            /// Base URI for alarm &amp; event adapter features.
            /// </summary>
            public const string BaseUri = "asc:features/events/";

            /// <summary>
            /// URI for <see cref="IEventMessagePush"/>.
            /// </summary>
            public const string EventMessagePush = BaseUri + "push/";

            /// <summary>
            /// URI for <see cref="IEventMessagePushWithTopics"/>.
            /// </summary>
            public const string EventMessagePushWithTopics = BaseUri + "topics/";

            /// <summary>
            /// URI for <see cref="IReadEventMessagesForTimeRange"/>.
            /// </summary>
            public const string ReadEventMessagesForTimeRange = BaseUri + "read/time/";

            /// <summary>
            /// URI for <see cref="IReadEventMessagesUsingCursor"/>.
            /// </summary>
            public const string ReadEventMessagesUsingCursor = BaseUri + "read/cursor/";

            /// <summary>
            /// URI for <see cref="IWriteEventMessages"/>.
            /// </summary>
            public const string WriteEventMessages = BaseUri + "write/";

        }


        /// <summary>
        /// Defines URIs for well-known real-time data adapter features.
        /// </summary>
        public static class RealTimeData {

            /// <summary>
            /// Base URI for real-time data adapter features.
            /// </summary>
            public const string BaseUri = "asc:features/real-time-data/";

            /// <summary>
            /// URI for <see cref="IReadTagValueAnnotations"/>.
            /// </summary>
            public const string ReadAnnotations = BaseUri + "annotations/read/";

            /// <summary>
            /// URI for <see cref="IReadPlotTagValues"/>.
            /// </summary>
            public const string ReadPlotTagValues = BaseUri + "values/read/plot/";

            /// <summary>
            /// URI for <see cref="IReadProcessedTagValues"/>.
            /// </summary>
            public const string ReadProcessedTagValues = BaseUri + "values/read/processed/";

            /// <summary>
            /// URI for <see cref="IReadRawTagValues"/>.
            /// </summary>
            public const string ReadRawTagValues = BaseUri + "values/read/raw/";

            /// <summary>
            /// URI for <see cref="IReadSnapshotTagValues"/>.
            /// </summary>
            public const string ReadSnapshotTagValues = BaseUri + "values/read/snapshot/";

            /// <summary>
            /// URI for <see cref="IReadTagValuesAtTimes"/>.
            /// </summary>
            public const string ReadTagValuesAtTimes = BaseUri + "values/read/at-times/";

            /// <summary>
            /// URI for <see cref="ISnapshotTagValuePush"/>.
            /// </summary>
            public const string SnapshotTagValuePush = BaseUri + "values/push/";

            /// <summary>
            /// URI for <see cref="IWriteTagValueAnnotations"/>.
            /// </summary>
            public const string WriteAnnotations = BaseUri + "annotations/write/";

            /// <summary>
            /// URI for <see cref="IWriteHistoricalTagValues"/>.
            /// </summary>
            public const string WriteHistoricalTagValues = BaseUri + "values/write/history/";

            /// <summary>
            /// URI for <see cref="IWriteSnapshotTagValues"/>.
            /// </summary>
            public const string WriteSnapshotTagValues = BaseUri + "values/write/snapshot/";

        }


        /// <summary>
        /// Defines URIs for well-known tag-related adapter features.
        /// </summary>
        public static class Tags {

            /// <summary>
            /// Base URI for tag-related adapter features.
            /// </summary>
            public const string BaseUri = "asc:features/tags/";

            /// <summary>
            /// URI for <see cref="ITagInfo"/>.
            /// </summary>
            public const string TagInfo = BaseUri + "info/";

            /// <summary>
            /// URI for <see cref="ITagSearch"/>.
            /// </summary>
            public const string TagSearch = BaseUri + "search/";

        }


        /// <summary>
        /// Defines URIs related to extension features.
        /// </summary>
        public static class Extensions {

            /// <summary>
            /// URI for <see cref="ICustomFunctions"/>.
            /// </summary>
            public const string CustomFunctions = "asc:features/extensions/custom-functions/";

            /// <summary>
            /// The root URI for all extension features (i.e. features extension <see cref="IAdapterExtensionFeature"/>).
            /// </summary>
            public const string BaseUri = "asc:extensions/";

        }

    }

#pragma warning restore CA1724 // Type names should not match namespaces

}
