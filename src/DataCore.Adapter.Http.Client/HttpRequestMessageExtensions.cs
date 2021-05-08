using System;
using System.Net.Http;

namespace DataCore.Adapter.Http.Client {

    /// <summary>
    /// Extension methods for <see cref="HttpRequestMessage"/>.
    /// </summary>
    public static class HttpRequestMessageExtensions {

        /// <summary>
        /// Associates metadata with the request.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="metadata">
        ///   The metadata to associate with the request.
        /// </param>
        /// <returns>
        ///   The <paramref name="request"/>.
        /// </returns>
        public static HttpRequestMessage AddRequestMetadata(this HttpRequestMessage request, RequestMetadata? metadata) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            if (metadata?.Headers != null) {
                foreach (var item in metadata.Headers) {
                    request.Headers.Add(item.Key, item.Value);
                }
            }

            return request.AddStateProperty(metadata);
        }


        /// <summary>
        /// Removes metadata that was previously associated with the request.
        /// </summary>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   The <paramref name="request"/>.
        /// </returns>
        public static HttpRequestMessage RemoveRequestMetadata(this HttpRequestMessage request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            return request.RemoveStateProperty();
        }

    }
}
