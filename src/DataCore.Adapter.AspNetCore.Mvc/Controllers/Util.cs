using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
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
        /// HTTP response header that indicates that a query generated too many results to return.
        /// </summary>
        private const string IncompleteResponseHeaderName = "DataCore-IncompleteResponse";

        /// <summary>
        /// Adds a header to an HTTP response to indicate that a query returned too many 
        /// items to include in a response object.
        /// </summary>
        /// <param name="response">
        ///   The response.
        /// </param>
        /// <param name="reason">
        ///   An explanation of the limit.
        /// </param>
        internal static void AddIncompleteResponseHeader(HttpResponse response, string reason) {
            response.Headers.Add(IncompleteResponseHeaderName, reason);
        }


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
        /// <returns>
        ///   An <see cref="IActionResult"/> that will contain the streamed items.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   <see cref="StreamResultsAsync{T}(IAsyncEnumerable{T}, Activity?)"/> will enumerate the 
        ///   first item in the <paramref name="stream"/> in case evaluating the stream throws an 
        ///   exception. If no exception is thrown, an <see cref="OkObjectResult"/> containing the 
        ///   streamed items will be returned. ASP.NET Core automatically chunks <see cref="OkObjectResult"/> 
        ///   instances that return an <see cref="IAsyncEnumerable{T}"/>.
        /// </para>
        /// 
        /// <para>
        ///   If evaluation of the first <paramref name="stream"/> item throws a <see cref="SecurityException"/>, 
        ///   a new <see cref="ForbidResult"/> will be returned.
        /// </para>
        /// 
        /// <para>
        ///   The provided <paramref name="activity"/> will always be disposed once the <paramref name="stream"/> 
        ///   has completed.
        /// </para>
        /// 
        /// </remarks>
        internal static async ValueTask<IActionResult> StreamResultsAsync<T>(
            IAsyncEnumerable<T> stream,
            Activity? activity
        ) {
            try {
                var enumerator = stream.ConfigureAwait(false).GetAsyncEnumerator();
                await enumerator.MoveNextAsync();
                return new OkObjectResult(EnumerateAsync(enumerator, activity));
            }
            catch (OperationCanceledException) {
                activity?.Dispose();
                return new StatusCodeResult(0);
            }
            catch (SecurityException) {
                activity?.Dispose();
                return new ForbidResult();
            }
            catch (Exception) {
                activity?.Dispose();
                throw;
            }
        }


        /// <summary>
        /// Generates a new <see cref="IAsyncEnumerable{T}"/> from a <see cref="ConfiguredCancelableAsyncEnumerable{T}.Enumerator"/> 
        /// and disposes of the provided <paramref name="activity"/> once the <paramref name="enumerator"/> 
        /// has completed.
        /// </summary>
        /// <typeparam name="T">
        ///   The value type.
        /// </typeparam>
        /// <param name="enumerator">
        ///   The <see cref="ConfiguredCancelableAsyncEnumerable{T}.Enumerator"/> to enumerate.
        /// </param>
        /// <param name="activity">
        ///   The <see cref="Activity"/> associated with the <paramref name="enumerator"/>.
        /// </param>
        /// <returns>
        ///   A new <see cref="IAsyncEnumerable{T}"/>.
        /// </returns>
        private static async IAsyncEnumerable<T> EnumerateAsync<T>(
            ConfiguredCancelableAsyncEnumerable<T>.Enumerator enumerator, 
            Activity? activity
        ) {
            var itemCount = 0;

            try {
                if (enumerator.Current != null) {
                    ++itemCount;
                    yield return enumerator.Current;
                }

                while (await enumerator.MoveNextAsync()) {
                    if (enumerator.Current != null) {
                        ++itemCount;
                        yield return enumerator.Current;
                    }
                }
            }
            finally {
                activity.SetResponseItemCountTag(itemCount);
                activity?.Dispose();
            }
        }

    }
}
