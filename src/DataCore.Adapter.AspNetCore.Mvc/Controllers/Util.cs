using System;

using Microsoft.AspNetCore.Http;

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

    }
}
