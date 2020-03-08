namespace DataCore.Adapter.Configuration {

    /// <summary>
    /// A schema that describes the settings that can be configured for an adapter.
    /// </summary>
    public class AdapterOptionsSchema {

        /// <summary>
        /// Content type for JSON schemas.
        /// </summary>
        public const string JsonSchema = "application/schema+json";

        /// <summary>
        /// The content type of the schema. <see cref="JsonSchema"/> is assumed by default.
        /// </summary>
        public string ContentType { get; set; } = JsonSchema;

        /// <summary>
        /// The schema.
        /// </summary>
        public string Schema { get; set; }

    }
}
