using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DataCore.Adapter.AspNetCore.Controllers {

    /// <summary>
    /// Utility functions for Web API controllers.
    /// </summary>
    internal static class Util {

        /// <summary>
        /// HTTP request header that is used to manage state for mutable topic-based subscriptions 
        /// (such as snapshot tag value subscriptions).
        /// </summary>
        internal const string SubscriptionIdHeaderName = "X-SubscriptionId";


        /// <summary>
        /// Converts the specified <see cref="DateTime"/> to UTC if it is not alreadt a UTC 
        /// timestamp.
        /// </summary>
        /// <param name="dt">
        ///   The <see cref="DateTime"/> instance.
        /// </param>
        /// <returns>
        ///   The equivalent UTC <see cref="DateTime"/> instance.
        /// </returns>
        /// <remarks>
        ///   If the <see cref="DateTime.Kind"/> for the <paramref name="dt"/> parameter is 
        ///   <see cref="DateTimeKind.Unspecified"/>, it will be treated as if it is UTC. A 
        ///   <see cref="DateTime"/> instance with the same <see cref="DateTime.Ticks"/> value but 
        ///   with a <see cref="DateTime.Kind"/> value of <see cref="DateTimeKind.Utc"/> will be 
        ///   returned.
        /// </remarks>
        internal static DateTime ConvertToUniversalTime(DateTime dt) {
            if (dt.Kind == DateTimeKind.Utc) {
                return dt;
            }

            if (dt.Kind == DateTimeKind.Local) {
                return dt.ToUniversalTime();
            }

            // Unspecified kind; assume that it is actually UTC.
            return new DateTime(dt.Ticks, DateTimeKind.Utc);
        }


        /// <summary>
        /// Creates an <see cref="IActionResult"/> that will stream the specified <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The <see cref="IAsyncEnumerable{T}"/> to return.
        /// </param>
        /// <param name="activity">
        ///   The <see cref="Activity"/> associated with reading the <paramref name="stream"/>.
        /// </param>
        /// <param name="onBeforeSendHeaders">
        ///   A callback to invoke before writing of the <paramref name="stream"/> to the response 
        ///   begins.
        /// </param>
        /// <param name="onCompleted">
        ///   A callback to invoke when the <paramref name="stream"/> has completed.
        /// </param>
        /// <returns>
        ///   An <see cref="IActionResult"/> that will contain the streamed items.
        /// </returns>
        /// <remarks>
        ///   The provided <paramref name="activity"/> will always be disposed once the <paramref name="stream"/> 
        ///   has completed.
        /// </remarks>
        internal static IActionResult StreamResults<T>(
            IAsyncEnumerable<T> stream,
            Activity? activity = null,
            Action? onBeforeSendHeaders = null,
            Action? onCompleted = null
        ) {
            try {
                onBeforeSendHeaders?.Invoke();
                return new OkObjectResult(EnumerateAsync(stream, activity, onCompleted));
            }
            catch (OperationCanceledException) {
                activity?.Dispose();
                onCompleted?.Invoke();
                return new StatusCodeResult(0);
            }
            catch (SecurityException) {
                activity?.Dispose();
                onCompleted?.Invoke();
                return new ForbidResult();
            }
            catch (Exception) {
                activity?.Dispose();
                onCompleted?.Invoke();
                throw;
            }
        }


        /// <summary>
        /// Enumerates the specified <paramref name="stream"/>.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The <see cref="IAsyncEnumerable{T}"/> to enumerate.
        /// </param>
        /// <param name="activity">
        ///   The <see cref="Activity"/> associated with the <paramref name="stream"/>.
        /// </param>
        /// <param name="onCompleted">
        ///   A callback to invoke when the <paramref name="stream"/> has completed.
        /// </param>
        /// <returns>
        ///   A new <see cref="IAsyncEnumerable{T}"/>.
        /// </returns>
        private static async IAsyncEnumerable<T> EnumerateAsync<T>(
            IAsyncEnumerable<T> stream,
            Activity? activity,
            Action? onCompleted
        ) {
            var itemCount = 0;

            try {
                await foreach (var item in stream.ConfigureAwait(false)) {
                    if (item == null) {
                        continue;
                    }
                    ++itemCount;
                    yield return item;
                }
            }
            finally {
                activity.SetResponseItemCountTag(itemCount);
                activity?.Dispose();
                onCompleted?.Invoke();
            }
        }

    }
}
