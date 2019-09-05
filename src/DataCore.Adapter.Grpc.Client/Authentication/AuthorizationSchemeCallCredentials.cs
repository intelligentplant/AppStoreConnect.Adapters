using System;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Client.Authentication {

    /// <summary>
    /// Describes call credentials that add an <c>Authorization</c> metadata entry to an outgoing 
    /// request.
    /// </summary>
    /// <seealso cref="BearerTokenCallCredentials"/>
    public class AuthorizationSchemeCallCredentials : IClientCallCredentials {

        /// <summary>
        /// The metadata key.
        /// </summary>
        public const string Key = "Authorization";

        /// <summary>
        /// The authorization scheme.
        /// </summary>
        private readonly string _scheme;

        /// <summary>
        /// The authorization value.
        /// </summary>
        private readonly string _value;


        /// <summary>
        /// Creates a new <see cref="AuthorizationSchemeCallCredentials"/> object.
        /// </summary>
        /// <param name="scheme">
        ///   The authorization scheme.
        /// </param>
        /// <param name="value">
        ///   The authorization value.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="scheme"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        protected AuthorizationSchemeCallCredentials(string scheme, string value) {
            _scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }


        /// <inheritdoc/>
        public Metadata.Entry GetMetadataEntry() {
            return new Metadata.Entry(Key, _scheme + " " + _value);
        }
    }
}
