using System;
using System.Diagnostics;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Diagnostics.Extensions {

    /// <summary>
    /// Extensions for <see cref="ActivitySource"/> related to custom functions.
    /// </summary>
    public static  class CustomFunctionsActivitySourceExtensions {

        /// <summary>
        /// Starts an activity associated with an <see cref="ICustomFunctions.GetFunctionsAsync"/> 
        /// call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static Activity? StartGetCustomFunctionsActivity(
            this ActivitySource source,
            string adapterId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(ICustomFunctions), nameof(ICustomFunctions.GetFunctionsAsync)),
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
        /// Starts an activity associated with an <see cref="ICustomFunctions.GetFunctionAsync"/> 
        /// call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="functionId"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static Activity? StartGetCustomFunctionActivity(
            this ActivitySource source,
            string adapterId,
            Uri functionId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }
            if (functionId == null) {
                throw new ArgumentNullException(nameof(functionId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(ICustomFunctions), nameof(ICustomFunctions.GetFunctionAsync)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetTagWithNamespace("extensions.function_id", functionId);

            return result;
        }


        /// <summary>
        /// Starts an activity associated with an <see cref="ICustomFunctions.InvokeFunctionAsync"/> 
        /// call.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="adapterId"></param>
        /// <param name="functionId"></param>
        /// <param name="kind"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static Activity? StartInvokeCustomFunctionActivity(
            this ActivitySource source,
            string adapterId,
            Uri functionId,
            ActivityKind kind = ActivityKind.Internal,
            string? parentId = null
        ) {
            if (string.IsNullOrWhiteSpace(adapterId)) {
                throw new ArgumentException(SharedResources.Error_IdIsRequired, nameof(adapterId));
            }
            if (functionId == null) {
                throw new ArgumentNullException(nameof(functionId));
            }

            var result = source.StartActivity(
                ActivitySourceExtensions.GetActivityName(typeof(ICustomFunctions), nameof(ICustomFunctions.InvokeFunctionAsync)),
                kind,
                parentId!
            );

            if (result == null) {
                return null;
            }

            result.SetAdapterTag(adapterId);
            result.SetTagWithNamespace("extensions.function_id", functionId);

            return result;
        }

    }

}
