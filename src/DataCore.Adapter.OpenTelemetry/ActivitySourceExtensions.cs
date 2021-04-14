using System;
using System.Diagnostics;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Extension methods for <see cref="ActivitySource"/>.
    /// </summary>
    public static partial class ActivitySourceExtensions {

        /// <summary>
        /// The name of the App Store Connect adapters activity source.
        /// </summary>
        public const string DiagnosticSourceName = "IntelligentPlant.AppStoreConnect.Adapter";

        /// <summary>
        /// The default namespace to use for OpenTelemetry attributes.
        /// </summary>
        public const string DefaultOpenTelemetryNamespace = "intelligentplant.appstoreconnect";


        /// <summary>
        /// Gets the activity name to use for the specified feature type and operation name.
        /// </summary>
        /// <param name="featureType">
        ///   The feature type.
        /// </param>
        /// <param name="operationName">
        ///   The operation name.
        /// </param>
        /// <returns>
        ///   The activity name.
        /// </returns>
        public static string GetActivityName(Type featureType, string operationName) {
            return string.Concat(DefaultOpenTelemetryNamespace, ".", "adapter", "/", featureType.Name, "/", operationName);
        }


        /// <summary>
        /// Creates a full-qualified name for use in e.g. an <see cref="Activity"/> tag.
        /// </summary>
        /// <param name="name">
        ///   The unqualified name.
        /// </param>
        /// <param name="namespace">
        ///   The namespace.
        /// </param>
        /// <returns>
        ///   The qualified name. If <paramref name="namespace"/> is <see langword="null"/> or 
        ///   white space, <see cref="DefaultOpenTelemetryNamespace"/> will be used as the namespace.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is <see langword="null"/> or white space.
        /// </exception>
        public static string GetQualifiedName(string name, string? @namespace) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException(SharedResources.Error_NameIsRequired, nameof(name));
            }

            return string.Concat(string.IsNullOrWhiteSpace(@namespace) ? DefaultOpenTelemetryNamespace : @namespace, ".", name);
        }


        /// <summary>
        /// Adds a tag to an activity.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type for the tag.
        /// </typeparam>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="name">
        ///   The unqualified tag name.
        /// </param>
        /// <param name="value">
        ///   The tag value.
        /// </param>
        /// <param name="namespace">
        ///   The namespace for the tag. Specify <see langword="null"/> or white space to use 
        ///   <see cref="DefaultOpenTelemetryNamespace"/>.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetTagWithNamespace<T>(this Activity? activity, string name, T? value, string? @namespace = null) {
            if (activity == null) {
                return null;
            }

            activity.SetTag(GetQualifiedName(name, @namespace), value);

            return activity;
        }


        /// <summary>
        /// Adds an adapter ID tag to an activity.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="adapter"/> is <see langword="null"/>.
        /// </exception>
        public static Activity? SetAdapterTag(this Activity? activity, IAdapter adapter) {
            if (adapter == null) {
                throw new ArgumentNullException(nameof(adapter));
            }
            return activity.SetAdapterTag(adapter.Descriptor.Id);
        }


        /// <summary>
        /// Adds an adapter ID tag to an activity.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        internal static Activity? SetAdapterTag(this Activity? activity, string adapterId) {
            if (activity == null) {
                return null;
            }

            return activity.SetTagWithNamespace("adapter_id", adapterId);
        }


        /// <summary>
        /// Sets a flag on an activity to mark it as a long-running subscription.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        internal static Activity? SetSubscriptionFlag(this Activity? activity) {
            if (activity == null) {
                return null;
            }

            return activity.SetTagWithNamespace("subscription", true);
        }


        /// <summary>
        /// Adds a publish interval tag to an activity.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="value">
        ///   The publish interval.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetPublishIntervalTag(this Activity? activity, TimeSpan value) {
            return activity.SetTagWithNamespace("publish_interval", value);
        }


        /// <summary>
        /// Adds query time range tags to an activity.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="start">
        ///   The UTC timestamp of the query start time.
        /// </param>
        /// <param name="end">
        ///   The UTC timestamp of the query end time.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetQueryTimeRangeTags(this Activity? activity, DateTime start, DateTime end) {
            if (activity == null) {
                return null;
            }
            return activity.SetQueryStartTimeTag(start).SetQueryEndTimeTag(end);
        }


        /// <summary>
        /// Adds a query time range start time tag to an activity.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="value">
        ///   The UTC timestamp of the query start time.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetQueryStartTimeTag(this Activity? activity, DateTime value) {
            return activity.SetTagWithNamespace("utc_query_start_time", value);
        }


        /// <summary>
        /// Adds a query time range start time tag to an activity.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="value">
        ///   The UTC timestamp of the query start time.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetQueryEndTimeTag(this Activity? activity, DateTime value) {
            return activity.SetTagWithNamespace("utc_query_end_time", value);
        }


        /// <summary>
        /// Adds a tag to an activity specifying the number of items (such as tags or asset model 
        /// nodes) that are being requested in a get-by-ID query.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="count">
        ///   The requested item count.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetRequestItemCountTag(this Activity? activity, int count) {
            return activity.SetTagWithNamespace("request_item_count", count);
        }


        /// <summary>
        /// Adds a tag to an activity specifying the number of items (such as tags or asset model 
        /// nodes) that are being requested in a get-by-ID query.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="count">
        ///   The requested item count.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetRequestItemCountTag(this Activity? activity, long count) {
            return activity.SetTagWithNamespace("request_item_count", count);
        }


        /// <summary>
        /// Adds a tag to an activity specifying the number of items (such as tags, asset model 
        /// nodes, tag values) that were returned by a query.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="count">
        ///   The requested item count.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetResponseItemCountTag(this Activity? activity, int count) {
            return activity.SetTagWithNamespace("response_item_count", count);
        }


        /// <summary>
        /// Adds a tag to an activity specifying the number of items (such as tags, asset model 
        /// nodes, tag values) that were returned by a query.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="count">
        ///   The requested item count.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetResponseItemCountTag(this Activity? activity, long count) {
            return activity.SetTagWithNamespace("response_item_count", count);
        }


        /// <summary>
        /// Adds tags to an activity describing the paging settings for a request.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetPagingTags(this Activity? activity, IPageableAdapterRequest? request) {
            if (activity == null) {
                return null;
            }

            if (request == null) {
                return activity;
            }

            return activity.SetPageSizeTag(request.PageSize).SetPageNumberTag(request.Page);
        }


        /// <summary>
        /// Adds a page size tag to an activity.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="value">
        ///   The page size.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetPageSizeTag(this Activity? activity, int value) {
            if (activity == null) {
                return null;
            }

            return activity.SetTagWithNamespace("page_size", value);
        }


        /// <summary>
        /// Adds a page number tag to an activity.
        /// </summary>
        /// <param name="activity">
        ///   The activity.
        /// </param>
        /// <param name="value">
        ///   The page number.
        /// </param>
        /// <returns>
        ///   The activity.
        /// </returns>
        public static Activity? SetPageNumberTag(this Activity? activity, int value) {
            if (activity == null) {
                return null;
            }

            return activity.SetTagWithNamespace("page", value);
        }

    }
}
