using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// Extensions for <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public static class JsonSerializerOptionsExtensions {

        /// <summary>
        /// Registers adapter-related converted.
        /// </summary>
        /// <param name="converters">
        ///   The JSON serialization options.
        /// </param>
        public static void AddAdapterConverters(this IList<JsonConverter> converters) {
            if (converters == null) {
                throw new ArgumentNullException(nameof(converters));
            }

            converters.Add(new VariantConverter());
        }


        /// <summary>
        /// Applies the serializer's naming policy to the specified property name.
        /// </summary>
        /// <param name="options">
        ///   The serializer options.
        /// </param>
        /// <param name="name">
        ///   The name to convert.
        /// </param>
        /// <returns>
        ///   The converted name. If the serializer options do not define a property naming policy, 
        ///   the original name is returned.
        /// </returns>
        internal static string ConvertPropertyName(this JsonSerializerOptions options, string name) {
            return options?.PropertyNamingPolicy?.ConvertName(name) ?? name;
        }

    }
}
