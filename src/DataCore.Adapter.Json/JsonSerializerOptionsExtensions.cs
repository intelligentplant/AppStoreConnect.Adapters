using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataCore.Adapter.Json {

    /// <summary>
    /// Extensions for <see cref="JsonSerializerOptions"/>.
    /// </summary>
    public static class JsonSerializerOptionsExtensions {

        /// <summary>
        /// Available converters.
        /// </summary>
        private static readonly JsonConverter[] s_converters;


        /// <summary>
        /// Class initializer.
        /// </summary>
        static JsonSerializerOptionsExtensions() {
            // Find all concrete JsonConverter types in this assembly, instantiate them, and 
            // assign them to the s_converters array.

            var jsonConverterType = typeof(JsonConverter);
            var converterTypes = typeof(JsonSerializerOptionsExtensions)
                .Assembly
                .GetTypes()
                .Where(x => x.IsClass)
                .Where(x => !x.IsAbstract)
                .Where(x => jsonConverterType.IsAssignableFrom(x));

            s_converters = converterTypes.Select(x => (JsonConverter) Activator.CreateInstance(x)!).ToArray();
        }


        /// <summary>
        /// Registers adapter-related converters.
        /// </summary>
        /// <param name="options">
        ///   The JSON serialization options.
        /// </param>
        public static void AddDataCoreAdapterConverters(this JsonSerializerOptions? options) {
            if (options == null) {
                return;
            }

            options.Converters.AddDataCoreAdapterConverters();
        }


        /// <summary>
        /// Registers adapter-related converters.
        /// </summary>
        /// <param name="converters">
        ///   The JSON converter collection.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="converters"/> is <see langword="null"/>.
        /// </exception>
        public static void AddDataCoreAdapterConverters(this IList<JsonConverter> converters) {
            if (converters == null) {
                throw new ArgumentNullException(nameof(converters));
            }

            // Add a string enum converter if one has not already been registered.

            if (!converters.OfType<JsonStringEnumConverter>().Any()) {
                converters.Add(new JsonStringEnumConverter());
            }

            foreach (var item in s_converters) {
                if (!converters.Contains(item)) {
                    converters.Add(item);
                }
            }
            
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
