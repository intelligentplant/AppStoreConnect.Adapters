using System;

using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;

namespace DataCore.Adapter.Json.Schema {

    /// <summary>
    /// <see cref="ISchemaGenerator"/> for <see cref="TimeSpan"/>.
    /// </summary>
    internal class TimeSpanSchemaGenerator : ISchemaGenerator {

        /// <inheritdoc/>
        public bool Handles(Type type) {
            return type == typeof(TimeSpan) || type == typeof(TimeSpan?);
        }


        /// <inheritdoc/>
        public void AddConstraints(SchemaGeneratorContext context) {
            context.Intents.Add(new TypeIntent(SchemaValueType.String));
            context.Intents.Add(new TimeSpanFormatIntent());
        }

    }
}
