using System;
using System.Collections.Generic;
using System.Text;

using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Proxy {

    /// <summary>
    /// Describes the call credentials to use when performing a gRPC call.
    /// </summary>
    public sealed class GrpcAdapterProxyCallCredentials {

        /// <summary>
        /// The authorization header name.
        /// </summary>
        private const string AuthorizationHeader = "Authorization";

        /// <summary>
        /// The bearer authentication scheme name.
        /// </summary>
        private const string BearerScheme = "Bearer";

        /// <summary>
        /// The metadata key for the credentials (e.g. <c>Authorization</c>).
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The authentication parameter (e.g. an authentication scheme and access token).
        /// </summary>
        public string Value { get;}


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxyCallCredentials"/> object.
        /// </summary>
        /// <param name="key">
        ///   The metadata key for the credentials (e.g. <c>Authorization</c>).
        /// </param>
        /// <param name="value">
        ///   The metadata value for the credentials (e.g. <c>Bearer some_access_token</c>).
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public GrpcAdapterProxyCallCredentials(string key, string value) {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }


        /// <summary>
        /// Creates a metadata entry for the credentials.
        /// </summary>
        /// <returns>
        ///   A gRPC metadata entry representing the credentials.
        /// </returns>
        internal GrpcCore.Metadata.Entry ToMetadataEntry() {
            return new GrpcCore.Metadata.Entry(Key, Value);
        }


        /// <summary>
        /// Creates a new <see cref="GrpcAdapterProxyCallCredentials"/> using the specified access token.
        /// </summary>
        /// <param name="accessToken">
        ///   The access token.
        /// </param>
        /// <returns>
        ///   The call credentials for the access token.
        /// </returns>
        public static GrpcAdapterProxyCallCredentials FromAccessToken(string accessToken) {
            if (accessToken == null) {
                throw new ArgumentNullException(nameof(accessToken));
            }

            return new GrpcAdapterProxyCallCredentials(AuthorizationHeader, BearerScheme + " " + accessToken);
        }

    }
}
