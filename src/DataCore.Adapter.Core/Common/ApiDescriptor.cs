using System.Text.Json.Serialization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Describes an API that can be used to query adapters (e.g. REST, gRPC, SignalR).
    /// </summary>
    public readonly struct ApiDescriptor {

        /// <summary>
        /// The API display name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The API provider, such as the library that implements the API.
        /// </summary>
        public string? Provider { get; }

        /// <summary>
        /// The version of the <see cref="Provider"/>.
        /// </summary>
        /// <remarks>
        ///   This is the version of the provider that implements the API (e.g. the version number 
        ///   of the implementing library), rather than the version of the API itself.
        /// </remarks>
        public string? Version { get; }

        /// <summary>
        /// Specifies if the API is enabled or not.
        /// </summary>
        public bool Enabled { get; }


        /// <summary>
        /// Creates a new <see cref="ApiDescriptor"/> instance.
        /// </summary>
        /// <param name="name">
        ///   The API display name.
        /// </param>
        /// <param name="provider">
        ///   The API provider, such as the library that implements the API.
        /// </param>
        /// <param name="version">
        ///   The version of the API provider. This is not the version of the API itself.
        /// </param>
        /// <param name="enabled">
        ///   Specifies if the API is enabled or not.
        /// </param>
        [JsonConstructor]
        public ApiDescriptor(string name, string? provider, string? version, bool enabled) {
            Name = name;
            Provider = provider;
            Version = version;
            Enabled = enabled;
        }

    }
}
