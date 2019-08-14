using System;
using System.Security.Cryptography.X509Certificates;
using Grpc.Core;

namespace DataCore.Adapter.Grpc.Client.Authentication {

    /// <summary>
    /// Describes call credentials that use an X.509 client certificate.
    /// </summary>
    public class ClientCertificateCallCredentials : IClientCallCredentials {

        /// <summary>
        /// The metadata key for the credentials.
        /// </summary>
        public const string Key = "X-ARR-ClientCert";

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
        public ClientCertificateCallCredentials(X509Certificate2 certificate) {
            _rawCertificate = certificate?.GetRawCertDataString() ?? throw new ArgumentNullException(nameof(certificate));
        }


        /// <inheritdoc/>
        public Metadata.Entry GetMetadataEntry() {
            return new Metadata.Entry(Key, _rawCertificate);
        }
    }
}
