using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataCore.Adapter.Http.Client {

    /// <summary>
    /// Extensions for <see cref="HttpResponseMessage"/>
    /// </summary>
    public static class HttpResponseMessageExtensions {

        /// <summary>
        /// Throws an <see cref="AdapterHttpClientException"/> if the response returned a non-good 
        /// status code.
        /// </summary>
        /// <param name="response">
        ///   The response.
        /// </param>
        /// <returns>
        ///   A task that will throw an <see cref="AdapterHttpClientException"/> if the response 
        ///   returned a non-good status code.
        /// </returns>
        public static async ValueTask ThrowOnErrorResponse(this HttpResponseMessage response) {
            if (response == null) {
                throw new ArgumentNullException(nameof(response));
            }
            if (response.IsSuccessStatusCode) {
                return;
            }

            var msg = string.Format(CultureInfo.CurrentCulture, Resources.Error_DefaultHttpErrorMessage, response.RequestMessage.Method.Method, response.RequestMessage.RequestUri, (int) response.StatusCode, response.ReasonPhrase);
            throw await AdapterHttpClientException.FromHttpResponseMessage(msg, response).ConfigureAwait(false);
        }

    }
}
