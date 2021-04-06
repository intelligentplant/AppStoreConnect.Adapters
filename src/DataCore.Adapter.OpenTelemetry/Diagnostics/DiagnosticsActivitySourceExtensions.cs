using System;
using System.Diagnostics;
using System.Linq;

namespace DataCore.Adapter.Diagnostics.Diagnostics { 

    /// <summary>
    /// Extensions for <see cref="ActivitySource"/> related to adapter diagnostic operations.
    /// </summary>
    public static class DiagnosticsActivitySourceExtensions {

    /// <summary>
    /// Starts an <see cref="Activity"/> associated with an 
    /// <see cref="IConfigurationChanges.Subscribe"/> call.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="adapterId"></param>
    /// <param name="request"></param>
    /// <param name="kind"></param>
    /// <param name="parentId"></param>
    /// <returns>
    ///   A new <see cref="Activity"/> instance, or <see langword="null"/> if the 
    ///   <paramref name="source"/> is not enabled.
    /// </returns>
    public static Activity? StartConfigurationChangesSubscribeActivity(
            this ActivitySource source,
            string adapterId,
            ConfigurationChangesSubscriptionRequest? request,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IConfigurationChanges), nameof(IConfigurationChanges.Subscribe)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            if (request != null) {
                result.SetRequestItemCountTag(request.ItemTypes?.Count() ?? 0);
            }

            return result;
        }


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IHealthCheck.CheckHealthAsync"/> call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns>
        ///   A new <see cref="Activity"/> instance, or <see langword="null"/> if the 
        ///   <paramref name="source"/> is not enabled.
        /// </returns>
        public static Activity? StartCheckHealthActivity(
            this ActivitySource source,
            string adapterId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IHealthCheck), nameof(IHealthCheck.CheckHealthAsync)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);

            return result;
        }


        /// <summary>
        /// Starts an <see cref="Activity"/> associated with an 
        /// <see cref="IHealthCheck.Subscribe"/> call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns>
        ///   A new <see cref="Activity"/> instance, or <see langword="null"/> if the 
        ///   <paramref name="source"/> is not enabled.
        /// </returns>
        public static Activity? StartHealthCheckSubscribeActivity(
            this ActivitySource source,
            string adapterId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(IHealthCheck), nameof(IHealthCheck.Subscribe)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetSubscriptionFlag();

            return result;
        }

    }
}
