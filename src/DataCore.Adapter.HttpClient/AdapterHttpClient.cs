using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Common.Models;
using DataCore.Adapter.Http.Clients;

namespace DataCore.Adapter.Http {

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
        /// The client for reading tag value annotations from and writing tag value annotations to 
        /// an adapter.
        /// </summary>
        public TagValueAnnotationsClient TagValueAnnotations { get; }

        /// <summary>
        /// The client for browsing tags on an adapter.
        /// </summary>
        public TagSearchClient TagSearch { get; }

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
            TagValueAnnotations = new TagValueAnnotationsClient(this);
            TagSearch = new TagSearchClient(this);
            TagValues = new TagValuesClient(this);
        }


    }
}
