#pragma warning disable CS0618 // Type or member is obsolete
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Http.Client.Clients;
using DataCore.Adapter.Json;

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
        /// The default HTTP version to use when making requests.
        /// </summary>
        /// <remarks>
        ///   When <see langword="null"/>, the default value of <see cref="HttpRequestMessage.Version"/> 
        ///   will be used.
        /// </remarks>
        public Version? DefaultRequestVersion { get; set; }

        /// <summary>
        /// JSON serializer options.
        /// </summary>
        internal JsonSerializerOptions JsonSerializerOptions { get; }

        /// <summary>
        /// The App Store Connect adapters toolkit version to use.
        /// </summary>
        public CompatibilityVersion CompatibilityVersion { get; set; } = CompatibilityVersion.Latest;

        /// <summary>
        /// The client for querying the remote host about available adapters.
        /// </summary>
        public AdaptersClient Adapters { get; }

        /// <summary>
        /// The client for querying an adapter's asset model.
        /// </summary>
        public AssetModelBrowserClient AssetModel { get; }

        /// <summary>
        /// The client for invoking custom adapter functions.
        /// </summary>
        public CustomFunctionsClient CustomFunctions { get; }

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
        [Obsolete(Adapter.Extensions.ExtensionFeatureConstants.ObsoleteMessage, Adapter.Extensions.ExtensionFeatureConstants.ObsoleteError)]
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
            JsonSerializerOptions = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }
                .AddDataCoreAdapterContext();

            Adapters = new AdaptersClient(this);
            AssetModel = new AssetModelBrowserClient(this);
            CustomFunctions = new CustomFunctionsClient(this);
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

            return new Jaahas.Http.HttpRequestPipelineHandler(async (request, next, cancellationToken) => {
                await callback(request, request.GetStateProperty<RequestMetadata>(), cancellationToken).ConfigureAwait(false);
                return await next(request, cancellationToken).ConfigureAwait(false);
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
        /// Gets the base relative API path to use.
        /// </summary>
        /// <returns>
        ///   The base relative API path for the client.
        /// </returns>
        internal string GetBaseUrl() {
            return CompatibilityVersion == CompatibilityVersion.Version_1_0
                ? "api/data-core/v1.0"
                : "api/app-store-connect/v2.0";
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
        /// <param name="version">
        ///   The HTTP version to use. When <see langword="null"/> is specified, the default value 
        ///   of <see cref="HttpRequestMessage.Version"/> is used.
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
        public static HttpRequestMessage CreateHttpRequestMessage(
            HttpMethod method, 
            Uri url, 
            Version? version,
            RequestMetadata? metadata
        ) {
            var message = new HttpRequestMessage(
                method ?? throw new ArgumentNullException(nameof(method)),
                url ?? throw new ArgumentNullException(nameof(url))
            ).AddRequestMetadata(metadata);

            if (version != null) {
                message.Version = version;
            }

            return message;
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
        public HttpRequestMessage CreateHttpRequestMessage(
            HttpMethod method,
            Uri url,
            RequestMetadata? metadata
        ) {
            return CreateHttpRequestMessage(method, url, DefaultRequestVersion, metadata);
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
        /// <param name="version">
        ///   The HTTP version to use. When <see langword="null"/> is specified, the default value 
        ///   of <see cref="HttpRequestMessage.Version"/> is used.
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
        public static HttpRequestMessage CreateHttpRequestMessage(
            HttpMethod method, 
            string url, 
            Version? version,
            RequestMetadata? metadata
        ) {
            return CreateHttpRequestMessage(
                method, 
                new Uri(url ?? throw new ArgumentNullException(nameof(url)), UriKind.RelativeOrAbsolute), 
                version,
                metadata
            );
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
        public HttpRequestMessage CreateHttpRequestMessage(
            HttpMethod method,
            string url,
            RequestMetadata? metadata
        ) {
            return CreateHttpRequestMessage(
                method,
                url,
                DefaultRequestVersion,
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
        /// <param name="version">
        ///   The HTTP version to use. When <see langword="null"/> is specified, the default value 
        ///   of <see cref="HttpRequestMessage.Version"/> is used.
        /// </param>
        /// <param name="metadata">
        ///   The request metadata.
        /// </param>
        /// <param name="options">
        ///   The JSON serializer options to use.
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
        public static HttpRequestMessage CreateHttpRequestMessage<TContent>(
            HttpMethod method, 
            Uri url, 
            TContent content, 
            Version? version,
            RequestMetadata? metadata,
            JsonSerializerOptions? options
        ) {
            var result = CreateHttpRequestMessage(method, url, version, metadata);
            result.Content = System.Net.Http.Json.JsonContent.Create(content, options: options);

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
        public HttpRequestMessage CreateHttpRequestMessage<TContent>(
            HttpMethod method,
            Uri url,
            TContent content,
            RequestMetadata? metadata
        ) {
            return CreateHttpRequestMessage(method, url, content, DefaultRequestVersion, metadata, JsonSerializerOptions);
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
        /// <param name="version">
        ///   The HTTP version to use. When <see langword="null"/> is specified, the default value 
        ///   of <see cref="HttpRequestMessage.Version"/> is used.
        /// </param>
        /// <param name="metadata">
        ///   The request metadata.
        /// </param>
        /// <param name="options">
        ///   The JSON serializer options to use.
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
        public static HttpRequestMessage CreateHttpRequestMessage<TContent>(
            HttpMethod method, 
            string url, 
            TContent content, 
            Version? version,
            RequestMetadata? metadata,
            JsonSerializerOptions? options
        ) {
            return CreateHttpRequestMessage(
                method,
                new Uri(url ?? throw new ArgumentNullException(nameof(url)), UriKind.RelativeOrAbsolute),
                content,
                version,
                metadata,
                options
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
        public HttpRequestMessage CreateHttpRequestMessage<TContent>(
            HttpMethod method,
            string url,
            TContent content,
            RequestMetadata? metadata
        ) {
            return CreateHttpRequestMessage(method, url, content, DefaultRequestVersion, metadata, JsonSerializerOptions);
        }

    }
}
#pragma warning restore CS0618 // Type or member is obsolete
