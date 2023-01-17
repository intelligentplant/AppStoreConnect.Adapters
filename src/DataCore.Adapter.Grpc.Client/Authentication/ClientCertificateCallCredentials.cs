using System;
using System.Security.Cryptography.X509Certificates;

using Grpc.Core;

namespace DataCore.Adapter.Grpc.Client.Authentication {

    /// <summary>
    /// Describes call credentials that use an X.509 client certificate. This credential type 
    /// works by setting a header (<c>X-ARR-ClientCert</c> by default) on outgoing requests. 
    /// See the remarks section for important notes regarding this credential type.
    /// </summary>
    /// <remarks>
    /// 
    /// <para>
    ///   If the remote server is configured to require client certificates (rather than just 
    ///   allowing them), this credential type will not successfully authenticate, due to 
    ///   authentication taking place at the TLS level rather than the request level. If client
    ///   certificates are required, they must be set directly on the gRPC channel or on the 
    ///   HTTP client that handles the underlying HTTP/2 connection (if you are using a managed 
    ///   gRPC client).
    /// </para>
    /// 
    /// <para>
    ///   In order to authenticate via this credential type when the web server allows client 
    ///   certificates, the web server must be configured to use client certificate forwarding 
    ///   (i.e. to extract the certificate from a request header and assign it to the client 
    ///   certificates for the incoming request). In ASP.NET Core, this is performed using the 
    ///   certificate forwarding service and middleware.
    /// </para>
    /// 
    /// </remarks>
    public class ClientCertificateCallCredentials : IClientCallCredentials {

        /// <summary>
        /// The default metadata key for the credentials.
        /// </summary>
        public const string DefaultKey = "X-ARR-ClientCert";

        /// <summary>
        /// The metadata key for the credentials.
        /// </summary>
        private readonly string _key;

        /// <summary>
        /// The raw certificate data.
        /// </summary>
        private readonly string _rawCertificate;


        /// <summary>
        /// Creates a new <see cref="ClientCertificateCallCredentials"/> object.
        /// </summary>
        /// <param name="certificate">
        ///   The certificate.
        /// </param>
        /// <remarks>
        ///   The credentials will be added using the <see cref="DefaultKey"/> metadata key.
        /// </remarks>
        public ClientCertificateCallCredentials(X509Certificate2 certificate)
            : this(certificate, null) { }


        /// <summary>
        /// Creates a new <see cref="ClientCertificateCallCredentials"/> object.
        /// </summary>
        /// <param name="certificate">
        ///   The certificate.
        /// </param>
        /// <param name="key">
        ///   The metadata key to use for the certificate.
        /// </param>
        /// <remarks>
        ///   If <paramref name="key"/> is <see langword="null"/> or white space, 
        ///   <see cref="DefaultKey"/> will be used.
        /// </remarks>
        public ClientCertificateCallCredentials(X509Certificate2 certificate, string? key) {
            var certBytes = certificate?.GetRawCertData() ?? throw new ArgumentNullException(nameof(certificate));
            _rawCertificate = Convert.ToBase64String(certBytes);
            _key = string.IsNullOrWhiteSpace(key)
                ? DefaultKey
                : key;
        }


        /// <inheritdoc/>
        public Metadata.Entry GetMetadataEntry() {
            return new Metadata.Entry(_key, _rawCertificate);
        }

    }
}
