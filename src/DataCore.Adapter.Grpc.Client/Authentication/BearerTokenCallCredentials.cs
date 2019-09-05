using System;

namespace DataCore.Adapter.Grpc.Client.Authentication {

    /// <summary>
    /// Describes call credentials that add a bearer token to an outgoing call's metadata.
    /// </summary>
    /// <seealso cref="AuthorizationSchemeCallCredentials"/>
    public class BearerTokenCallCredentials : AuthorizationSchemeCallCredentials {

        /// <summary>
        /// The bearer authentication scheme name.
        /// </summary>
        public const string Scheme = "Bearer";


        /// <summary>
        /// Creates a new <see cref="BearerTokenCallCredentials"/> object.
        /// </summary>
        /// <param name="token">
        ///   The bearer token.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="token"/> is <see langword="null"/>.
        /// </exception>
        public BearerTokenCallCredentials(string token) : base(Scheme, token) { }

    }
}
