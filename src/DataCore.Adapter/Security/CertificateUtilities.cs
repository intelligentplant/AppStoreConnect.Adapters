﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace DataCore.Adapter.Security {

    /// <summary>
    /// Utilities for working with certificates.
    /// </summary>
    public static class CertificateUtilities {

        /// <summary>
        /// Maximum line width for PEM encoding.
        /// </summary>
        private const int PemLineWidth = 64;

        /// <summary>
        /// PEM header/footer label for a certificate.
        /// </summary>
        private const string CertificateLabel = "CERTIFICATE";

        /// <summary>
        /// Regex for matching the full path to a certificate in a certificate store e.g. 
        /// <c>cert:\CurrentUser\My\1234567890abcdef1234567890abcdef123456789</c>. Both back- and 
        /// forward-slashes can be used as path separators.
        /// </summary>
        private static readonly Regex s_certPathWithThumbprintRegex = new Regex(@"^CERT\:(?:\\|/)(?<location>.+?)(?:\\|/)(?<store>.+)(?:\\|/)(?<thumbprint>.+)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Regex for matching a hex-encoded certificate thumbprint.
        /// </summary>
        private static readonly Regex s_thumbprintMatcher = new Regex(@"^[0-9A-F]+$", RegexOptions.IgnoreCase);


        /// <summary>
        /// PEM-encodes the specified bytes.
        /// </summary>
        /// <param name="bytes">
        ///   The bytes.
        /// </param>
        /// <param name="label">
        ///   The PEM header/footer label for the item.
        /// </param>
        /// <param name="explanatoryText">
        ///   Additional explanatory text to accompany the item.
        /// </param>
        /// <param name="builder">
        ///   The string builder to write the output to.
        /// </param>
        private static void PemEncode(byte[] bytes, string? label, IEnumerable<string>? explanatoryText, StringBuilder builder) {
            if (bytes == null) {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (explanatoryText != null) {
                foreach (var item in explanatoryText.Where(x => x != null)) {
                    builder.Append(item);
                    builder.Append('\n');
                }
            }

            var base64String = Convert.ToBase64String(bytes);

            if (!string.IsNullOrEmpty(label)) {
                builder.Append("-----BEGIN ");
                builder.Append(label);
                builder.Append("-----\n");
            }

            var i = 0;
            var @continue = false;

            do {
                ++i;
                var chars = base64String.Skip((i - 1) * PemLineWidth).Take(PemLineWidth);
                var lengthBefore = builder.Length;
                foreach (var c in chars) {
                    builder.Append(c);
                }
                @continue = builder.Length == lengthBefore + PemLineWidth;
                builder.Append('\n');
            } while (@continue);

            if (!string.IsNullOrEmpty(label)) {
                builder.Append("-----END ");
                builder.Append(label);
                builder.Append("-----\n");
            }
        }


        /// <summary>
        /// PEM-encodes the specified bytes.
        /// </summary>
        /// <param name="bytes">
        ///   The bytes to encode.
        /// </param>
        /// <param name="label">
        ///   Optional label to use (e.g. "RSA CERTIFICATE"). The encoded bytes will be prefixed 
        ///   and suffixed with <c>-----BEGIN {label}-----</c> and <c>-----END {label}-----</c>.
        /// </param>
        /// <param name="explanatoryText">
        ///   Optional lines of explanatory text add to the start of the output string (e.g. 
        ///   metadata about an X.509 certificate).
        /// </param>
        /// <returns>
        ///   The PEM-encoded bytes.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="bytes"/> is <see langword="null"/>.
        /// </exception>
        public static string PemEncode(byte[] bytes, string? label = null, IEnumerable<string>? explanatoryText = null) {
            var sb = new StringBuilder();
            PemEncode(bytes, label, explanatoryText, sb);
            return sb.ToString();
        }


        /// <summary>
        /// PEM-encodes a certificate, and optionally includes the full certificate chain.
        /// </summary>
        /// <param name="certificate">
        ///   The certificate to encode.
        /// </param>
        /// <param name="encodeChain">
        ///   <see langword="true"/> to encode the full certificate chain, or <see langword="false"/> 
        ///   to encode the specified certificate only.
        /// </param>
        /// <returns>
        ///   The encoded certificate(s).
        /// </returns>
        public static string PemEncode(X509Certificate2 certificate, bool encodeChain = false) {
            if (certificate == null) {
                throw new ArgumentNullException(nameof(certificate));
            }

            if (!encodeChain) {
                return PemEncode(new[] { certificate });
            }

            using (var chain = new X509Chain()) {
                chain.Build(certificate);

                try {
                    return PemEncode(chain.ChainElements.Cast<X509ChainElement>().Select(x => x.Certificate));
                }
                finally {
                    chain.Reset();
                }
            }
        }


        /// <summary>
        /// PEM-encodes the specified X.509 certificates.
        /// </summary>
        /// <param name="certificates">
        ///   The certificates to encode.
        /// </param>
        /// <returns>
        ///   The encoded certificates.
        /// </returns>
        public static string PemEncode(params X509Certificate2[] certificates) {
            return PemEncode((IEnumerable<X509Certificate2>) certificates);
        }


        /// <summary>
        /// PEM-encodes the specified X.509 certificates.
        /// </summary>
        /// <param name="certificates">
        ///   The certificates to encode.
        /// </param>
        /// <returns>
        ///   The encoded certificates.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="certificates"/> is <see langword="null"/>.
        /// </exception>
        public static string PemEncode(IEnumerable<X509Certificate2> certificates) {
            if (certificates == null) {
                throw new ArgumentNullException(nameof(certificates));
            }

            var builder = new StringBuilder();

            foreach (var certificate in certificates) {
                if (certificate == null) {
                    continue;
                }

                PemEncode(certificate.Export(X509ContentType.Cert), CertificateLabel, new[] {
                    $"# Subject: {certificate.Subject}",
                    $"# Issuer: {certificate.Issuer}",
                    $"# Friendly Name: {(certificate.FriendlyName ?? "<UNSPECIFIED>")}",
                    $"# Thumbprint: {certificate.Thumbprint}",
                    $"# Validity: from {certificate.NotBefore:yyyy-MM-ddTHH:mm:ss} UTC to {certificate.NotAfter:yyyy-MM-ddTHH:mm:ss} UTC",
                    string.Empty
                }, builder);

                builder.Append('\n');
                builder.Append('\n');
            }

            return builder.ToString();
        }


        /// <summary>
        /// Selects an appropriate certificate from the provided collection.
        /// </summary>
        /// <param name="certificates">
        ///   The certificates to select from.
        /// </param>
        /// <returns>
        ///   An appropriate <see cref="X509Certificate2"/>, or <see langword="null"/> if 
        ///   <paramref name="certificates"/>  is <see langword="null"/> or does not contain any 
        ///   entries
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   If <paramref name="certificates"/> contains multiple items, the following criteria 
        ///   will be applied (in order) to select the return value:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       If one or more certificates can be verified using basic validation policy (via 
        ///       <see cref="X509Certificate2.Verify"/>), the verified certificate with the latest 
        ///       <see cref="X509Certificate2.NotAfter"/> value will be returned.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       If one or more certificates can be verified using a validation policy that 
        ///       includes the <see cref="X509VerificationFlags.AllowUnknownCertificateAuthority"/>, 
        ///       the verified certificate with the latest <see cref="X509Certificate2.NotAfter"/> 
        ///       value will be returned. This allows valid self-signed certificates to be selected.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       If neither of the above two criteria return a result, the item from <paramref name="certificates"/> 
        ///       with the latest <see cref="X509Certificate2.NotAfter"/> value will be returned.
        ///     </description>
        ///   </item>
        /// </list>
        /// 
        /// </remarks>
        [Obsolete("This method will be removed in a future release.", false)]
        public static X509Certificate2? SelectCertificate(IEnumerable<X509Certificate2>? certificates) {
            // Order certificates in descending order of expiry time.
            var certs = certificates
                ?.Where(x => x != null)
                ?.OrderByDescending(x => x.NotAfter)
                ?.ToArray() ?? Array.Empty<X509Certificate2>();

            if (certs.Length == 0) {
                // No certificates specified; return null.
                return null;
            }

            if (certs.Length == 1) {
                // Only one certificate provided; return it.
                return certs[0];
            }

            // First pass: return the first valid certificate in the array.
            var certificate = certs.FirstOrDefault(x => x.Verify());
            if (certificate != null) {
                return certificate;
            }

            // Second pass: if we find an untrusted but otherwise valid certificate (e.g. self-signed), return it.
            using (var chain = new X509Chain()) {
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                foreach (var cert in certs) {
                    chain.Reset();
                    if (chain.Build(cert)) {
                        return cert;
                    }
                }
            }

            // Third pass: just return the first certificate in the array, even though it is
            // untrusted.
            return certs[0];
        }


        /// <summary>
        /// Tries to parse a certificate path string into the corresponding 
        /// <see cref="StoreLocation"/>, store name, and thumbprint or subject.
        /// </summary>
        /// <param name="path">
        ///   The certificate store path, in the case-insensitive format <c>cert:\{location}\{name}\{certificate_identifier}</c> 
        ///   or <c>cert:/{location}/{name}/{certificate_identifier}</c>.
        /// </param>
        /// <param name="location">
        ///   The store location.
        /// </param>
        /// <param name="name">
        ///   The store name.
        /// </param>
        /// <param name="thumbprintOrSubjectName">
        ///   The thumbprint or subject name for the certificate.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the path could be parsed, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        public static bool TryParseCertificateStorePath(string path, out StoreLocation location, out string name, out string thumbprintOrSubjectName) {
            if (path == null) {
                location = default;
                name = null!;
                thumbprintOrSubjectName = null!;
                return false;
            }

            var m = s_certPathWithThumbprintRegex.Match(path);
            if (!m.Success) {
                location = default;
                name = null!;
                thumbprintOrSubjectName = null!;
                return false;
            }
            
            var loc = m.Groups["location"].Value;
            if (!Enum.TryParse(loc, true, out location)) {
                location = default;
                name = null!;
                thumbprintOrSubjectName = null!;
                return false;
            }

            name = m.Groups["store"].Value;
            thumbprintOrSubjectName = m.Groups["thumbprint"].Value;
            return true;
        }


        /// <summary>
        /// Loads the certificate from the specified certificate store path.
        /// </summary>
        /// <param name="path">
        ///   The certificate store path, in the case-insensitive format <c>cert:\{location}\{name}\{certificate_identifier}</c> 
        ///   or <c>cert:/{location}/{name}/{certificate_identifier}</c>.
        /// </param>
        /// <param name="certificate">
        ///   The matching certificate.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the certificate could be loaded, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <remarks>
        ///   Calling <see cref="TryLoadCertificateFromStore(string, out X509Certificate2?)"/> is 
        ///   equivalent to calling <see cref="TryLoadCertificateFromStore(string, bool, bool, out X509Certificate2?)"/> 
        ///   with <see langword="false"/> specified for the <c>requirePrivateKey</c> and <c>allowInvalid</c> 
        ///   parameters.
        /// </remarks>
        [Obsolete("Use " + nameof(TryLoadCertificateFromStore) + "(string, bool, bool, out X509Certificate2?) instead.", false)]
        public static bool TryLoadCertificateFromStore(string path, out X509Certificate2? certificate) {
            return TryLoadCertificateFromStore(path, false, false, out certificate);
        }


        /// <summary>
        /// Loads the certificate from the specified certificate store path.
        /// </summary>
        /// <param name="path">
        ///   The certificate store path, in the case-insensitive format <c>cert:\{location}\{name}\{certificate_identifier}</c> 
        ///   or <c>cert:/{location}/{name}/{certificate_identifier}</c>.
        /// </param>
        /// <param name="requirePrivateKey">
        ///   <see langword="true"/> if the private key for the referenced certificate must be 
        ///   available from the store, or <see langword="false"/> if only the public certificate 
        ///   is required.
        /// </param>
        /// <param name="allowInvalid">
        ///   <see langword="true"/> to allow invalid (e.g. expired or untrusted) certificates to 
        ///   be returned, or <see langword="false"/> otherwise.
        /// </param>
        /// <param name="certificate">
        ///   The matching certificate.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the certificate could be loaded, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// <para>
        ///   The certificate identifier in the <paramref name="path"/> can be specified in one of the following ways:
        /// </para>
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     The hex-encoded thumbprint of the certificate (e.g. <c>cert:/CurrentUser/My/0123456789abcdef0123456789abcdef01234567</c>).
        ///   </item>
        ///   <item>
        ///     The full distinguished name for the certificate subject (e.g. <c>cert:/LocalMachine/My/CN=Adapter A, O=Intelligent Plant</c>).
        ///   </item>
        ///   <item>
        ///     A full or partial match for the simple name for the certificate subject (e.g. <c>cert:/LocalMachine/My/Adapter A</c>).
        ///   </item>
        /// </list>
        /// 
        /// <para>
        ///   When the simple name for the certificate is specified, the first certificate that 
        ///   exactly matches the simple name will be returned. If no exact match is found, the 
        ///   best candidate certificate that partially matches the simple name will be returned.
        /// </para>
        /// 
        /// </remarks>
        public static bool TryLoadCertificateFromStore(string path, bool requirePrivateKey, bool allowInvalid, out X509Certificate2? certificate) {
            if (path == null) {
                certificate = null;
                return false;
            }

            if (!TryParseCertificateStorePath(path, out var location, out var name, out var thumbprintOrSubjectName)) {
                certificate = null;
                return false;
            }

            using (var store = new X509Store(name, location)) {
                store.Open(OpenFlags.ReadOnly);

                if (s_thumbprintMatcher.IsMatch(thumbprintOrSubjectName)) {
                    certificate = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprintOrSubjectName, !allowInvalid)
                        .OfType<X509Certificate2>()
                        .Where(x => !requirePrivateKey || x.HasPrivateKey)
                        .OrderByDescending(x => x.NotAfter)
                        .FirstOrDefault();

                    if (certificate != null) {
                        return true;
                    }
                }

                X500DistinguishedName? distinguishedName = null;
                try {
                    distinguishedName = new X500DistinguishedName(thumbprintOrSubjectName);
                }
                catch { }

                if (distinguishedName != null) {
                    certificate = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, distinguishedName.Name, !allowInvalid)
                        .OfType<X509Certificate2>()
                        .Where(x => !requirePrivateKey || x.HasPrivateKey)
                        .OrderByDescending(x => x.NotAfter)
                        .FirstOrDefault();

                    return certificate != null;
                }
                else {
                    var certs = store.Certificates.Find(X509FindType.FindBySubjectName, thumbprintOrSubjectName, !allowInvalid)
                        .OfType<X509Certificate2>()
                        .Where(x => !requirePrivateKey || x.HasPrivateKey)
                        .OrderByDescending(x => x.NotAfter);

                    X509Certificate2? bestMatch = null;

                    foreach (var cert in certs) {
                        // The first certificate of all matching certificates will be returned unless
                        // we find an exact match later in the collection.
                        bestMatch ??= cert;

                        if (cert.GetNameInfo(X509NameType.SimpleName, true).Equals(thumbprintOrSubjectName, StringComparison.OrdinalIgnoreCase)) {
                            // Current certificate matches the requested subject name exactly.
                            certificate = cert;
                            return true;
                        }
                    }

                    certificate = bestMatch;
                    return certificate != null;
                }
            }
        }

    }
}
