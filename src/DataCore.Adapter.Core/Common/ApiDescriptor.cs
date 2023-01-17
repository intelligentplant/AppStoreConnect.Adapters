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
        /// The API version.
        /// </summary>
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
        /// <param name="version">
        ///   The API version.
        /// </param>
        /// <param name="enabled">
        ///   Specifies if the API is enabled or not.
        /// </param>
        [JsonConstructor]
        public ApiDescriptor(string name, string? version, bool enabled) {
            Name = name;
            Version = version;
            Enabled = enabled;
        }

    }
}
