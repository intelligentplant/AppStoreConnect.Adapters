using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;

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
        internal static IActionResult StreamResults<T>(
            IAsyncEnumerable<T> stream,
            Action? onBeforeSendHeaders = null,
            Action? onCompleted = null
        ) {
            try {
                onBeforeSendHeaders?.Invoke();
                return new OkObjectResult(EnumerateAsync(stream, onCompleted));
            }
            catch (OperationCanceledException) {
                onCompleted?.Invoke();
                return new StatusCodeResult(0);
            }
            catch (SecurityException) {
                onCompleted?.Invoke();
                return new ForbidResult();
            }
            catch (Exception) {
                onCompleted?.Invoke();
                throw;
            }
        }


        /// <summary>
        /// Creates an <see cref="IActionResult"/> that will stream the specified <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The value type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The transformed value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The <see cref="IAsyncEnumerable{TOut}"/> to enumerate.
        /// </param>
        /// <param name="transform">
        ///   A callback that will transform each <typeparamref name="TIn"/> emitted by 
        ///   <paramref name="stream"/> into an instance of <typeparamref name="TOut"/>.
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
        internal static IActionResult StreamResults<TIn, TOut>(
            IAsyncEnumerable<TIn> stream,
            Func<TIn, TOut> transform,
            Action? onBeforeSendHeaders = null,
            Action? onCompleted = null
        ) {
            try {
                onBeforeSendHeaders?.Invoke();
                return new OkObjectResult(EnumerateAsync(stream, transform, onCompleted));
            }
            catch (OperationCanceledException) {
                onCompleted?.Invoke();
                return new StatusCodeResult(0);
            }
            catch (SecurityException) {
                onCompleted?.Invoke();
                return new ForbidResult();
            }
            catch (Exception) {
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
        /// <param name="onCompleted">
        ///   A callback to invoke when the <paramref name="stream"/> has completed.
        /// </param>
        /// <returns>
        ///   A new <see cref="IAsyncEnumerable{T}"/>.
        /// </returns>
        private static async IAsyncEnumerable<T> EnumerateAsync<T>(
            IAsyncEnumerable<T> stream,
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
                onCompleted?.Invoke();
            }
        }


        /// <summary>
        /// Enumerates the specified <paramref name="stream"/>.
        /// </summary>
        /// <typeparam name="TIn">
        ///   The value type.
        /// </typeparam>
        /// <typeparam name="TOut">
        ///   The transformed value type.
        /// </typeparam>
        /// <param name="stream">
        ///   The <see cref="IAsyncEnumerable{T}"/> to enumerate.
        /// </param>
        /// <param name="transform">
        ///   A callback that will transform each <typeparamref name="TIn"/> emitted by 
        ///   <paramref name="stream"/> into an instance of <typeparamref name="TOut"/>.
        /// </param>
        /// <param name="onCompleted">
        ///   A callback to invoke when the <paramref name="stream"/> has completed.
        /// </param>
        /// <returns>
        ///   A new <see cref="IAsyncEnumerable{T}"/>.
        /// </returns>
        private static async IAsyncEnumerable<TOut> EnumerateAsync<TIn, TOut>(
            IAsyncEnumerable<TIn> stream,
            Func<TIn, TOut> transform,
            Action? onCompleted
        ) {
            var itemCount = 0;

            try {
                await foreach (var item in stream.ConfigureAwait(false)) {
                    if (item == null) {
                        continue;
                    }
                    ++itemCount;
                    yield return transform.Invoke(item);
                }
            }
            finally {
                onCompleted?.Invoke();
            }
        }

    }
}
