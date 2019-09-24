using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter.Grpc.Client.Authentication {

    /// <summary>
    /// Describes call credentials that add basic authentication credentials to an outgoing call's 
    /// metadata.
    /// </summary>
    /// <seealso cref="AuthorizationSchemeCallCredentials"/>
    public class BasicAuthenticationCallCredentials : AuthorizationSchemeCallCredentials {

        /// <summary>
        /// The Basic authentication scheme name.
        /// </summary>
        public const string Scheme = "Basic";


        /// <summary>
        /// Creates a new <see cref="BasicAuthenticationCallCredentials"/> object.
        /// </summary>
        /// <param name="userName">
        ///   The user name.
        /// </param>
        /// <param name="password">
        ///   The password.
        /// </param>
        public BasicAuthenticationCallCredentials(string userName, string password) : base(
            Scheme, 
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes(string.Concat(userName, ':', password))
            )
        ) { }

    }
}
