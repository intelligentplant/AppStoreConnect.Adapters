using System.Collections.Generic;
using System.Security.Claims;

namespace DataCore.Adapter.Http.Client {

    /// <summary>
    /// Describes metadata to associate with an adapter HTTP request.
    /// </summary>
    public class RequestMetadata {

        /// <summary>
        /// The principal associated with the request.
        /// </summary>
        public ClaimsPrincipal? Principal { get; set; }

        /// <summary>
        /// The correlation ID for the request. When specified, the <c>Request-Id</c> header on the 
        /// outgoing request will be set to this value.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Additional headers to add to the request.
        /// </summary>
        public IDictionary<string, string> Headers { get; }

        /// <summary>
        /// Additional items related to the request.
        /// </summary>
        public IDictionary<object, object?> Items { get; }


        /// <summary>
        /// Creates a new <see cref="RequestMetadata"/> object.
        /// </summary>
        /// <param name="principal">
        ///   The principal associated with the request.
        /// </param>
        /// <param name="correlationId">
        ///   The correlation ID for the request.
        /// </param>
        /// <param name="headers">
        ///   Additional headers to add to the request.
        /// </param>
        /// <param name="items">
        ///   Additional items related to the request.
        /// </param>
        public RequestMetadata(
            ClaimsPrincipal? principal = null, 
            string? correlationId = null, 
            IDictionary<string, string>? headers = null, 
            IDictionary<object, object?>? items = null
        ) {
            Principal = principal;
            CorrelationId = correlationId;
            Headers = headers == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(headers);
            Items = items == null
                ? new Dictionary<object, object?>()
                : new Dictionary<object, object?>(items);
        }

    }
}
