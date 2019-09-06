using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Http.Client.Clients;

namespace DataCore.Adapter.Http.Client {

    /// <summary>
    /// Client for querying remote adapters via HTTP.
    /// </summary>
    public class AdapterHttpClient {

        /// <summary>
        /// The HTTP client for the <see cref="AdapterHttpClient"/>.
        /// </summary>
        public HttpClient HttpClient { get; }

        /// <summary>
        /// The client for querying the remote host about available adapters.
        /// </summary>
        public AdaptersClient Adapters { get; }

        /// <summary>
        /// The client for querying an adapter's asset model.
        /// </summary>
        public AssetModelBrowserClient AssetModel { get; }

        /// <summary>
        /// The client for reading event messages from and writing event messages to an adapter.
        /// </summary>
        public EventsClient Events { get; }

        /// <summary>
        /// The client for requesting information about the remote host.
        /// </summary>
        public HostInfoClient HostInfo { get; }

        /// <summary>
        /// The client for browsing tags on an adapter.
        /// </summary>
        public TagSearchClient TagSearch { get; }

        /// <summary>
        /// The client for reading tag value annotations from and writing tag value annotations to 
        /// an adapter.
        /// </summary>
        public TagValueAnnotationsClient TagValueAnnotations { get; }

        /// <summary>
        /// The client for reading tag values from and writing tag values to an adapter.
        /// </summary>
        public TagValuesClient TagValues { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterHttpClient"/> object.
        /// </summary>
        /// <param name="httpClient">
        ///   The HTTP to use. The client must be configured to use the correct base URL for the 
        ///   remote adapter host. If authentication is required when querying the remote host, it 
        ///   is the responsibility of the HTTP client to set the appropriate HTTP headers on 
        ///   outgoing requests prior to sending them.
        /// </param>
        public AdapterHttpClient(HttpClient httpClient) {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            Adapters = new AdaptersClient(this);
            AssetModel = new AssetModelBrowserClient(this);
            Events = new EventsClient(this);
            HostInfo = new HostInfoClient(this);
            TagSearch = new TagSearchClient(this);
            TagValueAnnotations = new TagValueAnnotationsClient(this);
            TagValues = new TagValuesClient(this);
        }


        /// <summary>
        /// Creates an <see cref="HttpMessageHandler"/> that can be used to transform an outgoing 
        /// HTTP request based on a <see cref="ClaimsPrincipal"/> associated with the request. 
        /// This can be used to e.g. dynamically add an <c>Authorize</c> header or client 
        /// certificate to the request based on the identities of the <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="callback">
        ///   The callback to invoke prior to sending the request. The <see cref="ClaimsPrincipal"/> 
        ///   can be <see langword="null"/> if none was provided when invoking the adapter client 
        ///   method.
        /// </param>
        /// <returns>
        ///   A new <see cref="HttpMessageHandler"/> that can be added to the request pipeline for 
        ///   the <see cref="HttpClient"/> used with an <see cref="AdapterHttpClient"/> instance.
        /// </returns>
        public static DelegatingHandler CreateRequestTransformHandler(Func<HttpRequestMessage, ClaimsPrincipal, CancellationToken, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }

            return new Jaahas.Http.HttpRequestTransformHandler(async (request, cancellationToken) => {
                await callback(request, request.GetStateProperty<ClaimsPrincipal>(), cancellationToken).ConfigureAwait(false);
            });
        }


        /// <summary>
        /// Validates the specified object. This method should be called on any adapter request objects 
        /// prior to passing them to an adapter.
        /// </summary>
        /// <param name="instance">
        ///   The object to validate.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="instance"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="instance"/> is not valid.
        /// </exception>
        protected internal void ValidateObject(object instance) {
            if (instance == null) {
                throw new ArgumentNullException(nameof(instance));
            }

            Validator.ValidateObject(instance, new ValidationContext(instance), true);
        }

    }
}
