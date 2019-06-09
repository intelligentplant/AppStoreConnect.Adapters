using System;
using System.Collections.Generic;
using System.Text;
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

    }
}
