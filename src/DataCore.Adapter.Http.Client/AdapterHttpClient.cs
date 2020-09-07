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
        /// The client for invoking extension features on an adapter.
        /// </summary>
        public ExtensionFeaturesClient Extensions { get; }


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
            Extensions = new ExtensionFeaturesClient(this);
            HostInfo = new HostInfoClient(this);
            TagSearch = new TagSearchClient(this);
            TagValueAnnotations = new TagValueAnnotationsClient(this);
            TagValues = new TagValuesClient(this);
        }


        /// <summary>
        /// Creates an <see cref="HttpMessageHandler"/> that can be used to transform an outgoing 
        /// HTTP request based on a <see cref="RequestMetadata"/> object associated with the request. 
        /// This can be used to e.g. dynamically add an <c>Authorize</c> header or client 
        /// certificate to the request based on the identities of the <see cref="ClaimsPrincipal"/> 
        /// associated with the request.
        /// </summary>
        /// <param name="callback">
        ///   The callback to invoke prior to sending the request. The <see cref="RequestMetadata"/> 
        ///   can be <see langword="null"/> if none was provided when invoking the adapter client 
        ///   method.
        /// </param>
        /// <returns>
        ///   A new <see cref="HttpMessageHandler"/> that can be added to the request pipeline for 
        ///   the <see cref="System.Net.Http.HttpClient"/> used with an <see cref="AdapterHttpClient"/> 
        ///   instance.
        /// </returns>
        public static DelegatingHandler CreateRequestTransformHandler(Func<HttpRequestMessage, RequestMetadata, CancellationToken, Task> callback) {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }

            return new Jaahas.Http.HttpRequestTransformHandler(async (request, cancellationToken) => {
                await callback(request, request.GetStateProperty<RequestMetadata>(), cancellationToken).ConfigureAwait(false);
            });
        }


        /// <summary>
        /// Validates an object. This should be called on all adapter request objects prior to 
        /// invoking a remote endpoint.
        /// </summary>
        /// <param name="o">
        ///   The object.
        /// </param>
        /// <param name="canBeNull">
        ///   When <see langword="true"/>, validation will succeed if <paramref name="o"/> is 
        ///   <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="o"/> is <see langword="null"/> and <paramref name="canBeNull"/> is 
        ///   <see langword="false"/>.
        /// </exception>
        /// <exception cref="ValidationException">
        ///   <paramref name="o"/> fails validation.
        /// </exception>
        public static void ValidateObject(object o, bool canBeNull = false) {
            if (canBeNull && o == null) {
                return;
            }

            if (o == null) {
                throw new ArgumentNullException(nameof(o));
            }

            Validator.ValidateObject(o, new ValidationContext(o));
        }



        /// <summary>
        /// Creates a new <see cref="HttpRequestMessage"/> that has the specified metadata attached.
        /// </summary>
        /// <param name="method">
        ///   The request method.
        /// </param>
        /// <param name="url">
        ///   The request URL.
        /// </param>
        /// <param name="metadata">
        ///   The request metadata.
        /// </param>
        /// <returns>
        ///   A new <see cref="HttpRequestMessage"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="method"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Caller is responsible for disposing the object")]
        protected internal static HttpRequestMessage CreateHttpRequestMessage(
            HttpMethod method, 
            Uri url, 
            RequestMetadata metadata
        ) {
            return new HttpRequestMessage(
                method ?? throw new ArgumentNullException(nameof(method)),
                url ?? throw new ArgumentNullException(nameof(url))
            ).AddRequestMetadata(metadata);
        }


        /// <summary>
        /// Creates a new <see cref="HttpRequestMessage"/> that has the specified metadata attached.
        /// </summary>
        /// <param name="method">
        ///   The request method.
        /// </param>
        /// <param name="url">
        ///   The request URL.
        /// </param>
        /// <param name="metadata">
        ///   The request metadata.
        /// </param>
        /// <returns>
        ///   A new <see cref="HttpRequestMessage"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="method"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        protected internal static HttpRequestMessage CreateHttpRequestMessage(
            HttpMethod method, 
            string url, 
            RequestMetadata metadata
        ) {
            return CreateHttpRequestMessage(
                method, 
                new Uri(url ?? throw new ArgumentNullException(nameof(url)), UriKind.RelativeOrAbsolute), 
                metadata
            );
        }


        /// <summary>
        /// Creates a new <see cref="HttpRequestMessage"/> that has the specified content and metadata attached.
        /// </summary>
        /// <typeparam name="TContent">
        ///   The type of the <paramref name="content"/>.
        /// </typeparam>
        /// <param name="method">
        ///   The request method.
        /// </param>
        /// <param name="url">
        ///   The request URL.
        /// </param>
        /// <param name="content">
        ///   The content for the request. The content will be serialized to JSON.
        /// </param>
        /// <param name="metadata">
        ///   The request metadata.
        /// </param>
        /// <returns>
        ///   A new <see cref="HttpRequestMessage"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="method"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        protected internal static HttpRequestMessage CreateHttpRequestMessage<TContent>(
            HttpMethod method, 
            Uri url, 
            TContent content, 
            RequestMetadata metadata
        ) {
            var result = CreateHttpRequestMessage(method, url, metadata);
            result.Content = content is HttpContent httpContent
                ? httpContent
                : new ObjectContent(typeof(TContent), content, new System.Net.Http.Formatting.JsonMediaTypeFormatter());

            return result;
        }


        /// <summary>
        /// Creates a new <see cref="HttpRequestMessage"/> that has the specified content and metadata attached.
        /// </summary>
        /// <typeparam name="TContent">
        ///   The type of the <paramref name="content"/>.
        /// </typeparam>
        /// <param name="method">
        ///   The request method.
        /// </param>
        /// <param name="url">
        ///   The request URL.
        /// </param>
        /// <param name="content">
        ///   The content for the request. The content will be serialized to JSON.
        /// </param>
        /// <param name="metadata">
        ///   The request metadata.
        /// </param>
        /// <returns>
        ///   A new <see cref="HttpRequestMessage"/> object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="method"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        protected internal static HttpRequestMessage CreateHttpRequestMessage<TContent>(
            HttpMethod method, 
            string url, 
            TContent content, 
            RequestMetadata metadata
        ) {
            return CreateHttpRequestMessage(
                method,
                new Uri(url ?? throw new ArgumentNullException(nameof(url)), UriKind.RelativeOrAbsolute),
                content,
                metadata
            );
        }

    }
}
