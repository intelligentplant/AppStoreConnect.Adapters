using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// Extensions for <see cref="JsonSerializerSettings"/>.
    /// </summary>
    public static class JsonSerializerSettingsExtensions {

        /// <summary>
        /// Registers adapter-related converters.
        /// </summary>
        /// <param name="settings">
        ///   The JSON serialization settings.
        /// </param>
        public static void AddDataCoreAdapterConverters(this JsonSerializerSettings settings) {
            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.AddDataCoreAdapterConverters(null);

        }


        /// <summary>
        /// Registers adapter-related converters.
        /// </summary>
        /// <param name="converters">
        ///   The JSON converter collection to add new converters to.
        /// </param>
        public static void AddDataCoreAdapterConverters(this ICollection<JsonConverter> converters) {
            if (converters == null) {
                throw new ArgumentNullException(nameof(converters));
            }

            converters.AddDataCoreAdapterConverters(null);
        }


        /// <summary>
        /// Registers adapter-related converters.
        /// </summary>
        /// <param name="settings">
        ///   The JSON serialization settings.
        /// </param>
        /// <param name="jsonElementConverterOptions">
        ///   The <see cref="System.Text.Json.JsonSerializerOptions"/> to use with the <see cref="JsonElementConverter"/> 
        ///   registered by this method.
        /// </param>
        public static void AddDataCoreAdapterConverters(this JsonSerializerSettings settings, System.Text.Json.JsonSerializerOptions? jsonElementConverterOptions) {
            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Converters.AddDataCoreAdapterConverters(jsonElementConverterOptions);

        }


        /// <summary>
        /// Registers adapter-related converters.
        /// </summary>
        /// <param name="converters">
        ///   The JSON converter collection to add new converters to.
        /// </param>
        /// <param name="jsonElementConverterOptions">
        ///   The <see cref="System.Text.Json.JsonSerializerOptions"/> to use with the <see cref="JsonElementConverter"/> 
        ///   registered by this method.
        /// </param>
        public static void AddDataCoreAdapterConverters(this ICollection<JsonConverter> converters, System.Text.Json.JsonSerializerOptions? jsonElementConverterOptions) {
            if (converters == null) {
                throw new ArgumentNullException(nameof(converters));
            }

            converters.Add(new JsonElementConverter(jsonElementConverterOptions));
            converters.Add(new NullableJsonElementConverter(jsonElementConverterOptions));
            converters.Add(new VariantConverter());
            converters.Add(new ByteStringConverter());
        }

    }
}
