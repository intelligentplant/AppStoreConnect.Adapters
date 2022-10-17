using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataCore.Adapter.AspNetCore.SignalR.Client;
using DataCore.Adapter.Json;

using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Client {

    /// <summary>
    /// Extensions for <see cref="HubConnectionBuilder"/>.
    /// </summary>
    public static class AdapterHubConnectionBuilderExtensions {

        /// <summary>
        /// Configures the <see cref="HubConnectionBuilder"/> to connect to the adapter SignalR API 
        /// at the specified URL.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="HubConnectionBuilder"/>.
        /// </param>
        /// <param name="url">
        ///   The URL to connect to. In most circumstances, this will be the base URL of the 
        ///   adapter host combined with <see cref="AdapterSignalRClient.DefaultHubRoute"/>.
        /// </param>
        /// <param name="configureHttpConnection">
        ///   A callback to perform additional configuration of the SignalR client's 
        ///   <see cref="HttpConnectionOptions"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="HubConnectionBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   The <see cref="HubConnectionBuilder"/> will be configured to use JSON protocol. Due 
        ///   to the registration of a custom <see cref="JsonSerializerContext"/>, the <see cref="JsonSerializerOptions"/> 
        ///   for the JSON protocol cannot be modified after calling this method.
        /// </remarks>
        public static HubConnectionBuilder WithDataCoreAdapterConnection(this HubConnectionBuilder builder, Uri url, Action<HttpConnectionOptions>? configureHttpConnection = null) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (url == null) {
                throw new ArgumentNullException(nameof(url));
            }

            builder
                .WithUrl(url, options => {
                    configureHttpConnection?.Invoke(options);
                })
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.PayloadSerializerOptions.AddDataCoreAdapterContext();
                });

            return builder;
        }


        /// <summary>
        /// Configures the <see cref="HubConnectionBuilder"/> to connect to the adapter SignalR API 
        /// at the specified URL.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="HubConnectionBuilder"/>.
        /// </param>
        /// <param name="url">
        ///   The URL to connect to. In most circumstances, this will be the base URL of the 
        ///   adapter host combined with <see cref="AdapterSignalRClient.DefaultHubRoute"/>.
        /// </param>
        /// <param name="configureHttpConnection">
        ///   A callback to perform additional configuration of the SignalR client's 
        ///   <see cref="HttpConnectionOptions"/>.
        /// </param>
        /// <returns>
        ///   The <see cref="HubConnectionBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   The <see cref="HubConnectionBuilder"/> will be configured to use JSON protocol. Due 
        ///   to the registration of a custom <see cref="JsonSerializerContext"/>, the <see cref="JsonSerializerOptions"/> 
        ///   for the JSON protocol cannot be modified after calling this method.
        /// </remarks>
        public static HubConnectionBuilder WithDataCoreAdapterConnection(this HubConnectionBuilder builder, string url, Action<HttpConnectionOptions>? configureHttpConnection = null) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            if (url == null) {
                throw new ArgumentNullException(nameof(url));
            }

            builder
                .WithUrl(url, options => {
                    configureHttpConnection?.Invoke(options);
                })
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.AddDataCoreAdapterContext();
                });

            return builder;
        }

    }
}
