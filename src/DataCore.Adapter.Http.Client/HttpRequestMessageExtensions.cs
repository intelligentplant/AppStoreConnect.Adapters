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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestMessage RemoveRequestMetadata(this HttpRequestMessage request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            return request.RemoveStateProperty();
        }


        /// <summary>
        /// Adds a header to the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">
        ///   The <see cref="HttpRequestMessage"/>.
        /// </param>
        /// <param name="name">
        ///   The header name.
        /// </param>
        /// <param name="value">
        ///   The header value.
        /// </param>
        /// <returns>
        ///   The <see cref="HttpRequestMessage"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public static HttpRequestMessage WithHeader(this HttpRequestMessage request, string name, string value) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            request.Headers.Add(name, value);

            return request;
        }

    }
}
