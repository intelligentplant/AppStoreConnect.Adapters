using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DataCore.Adapter.NewtonsoftJson {

    /// <summary>
    /// Extensions for <see cref="JsonSerializerSettings"/>.
    /// </summary>
    public static class JsonSerializerSettingsExtensions {

        /// <summary>
        /// Registers adapter-related converters and default serialization behaviours.
        /// </summary>
        /// <param name="settings">
        ///   The JSON serialization settings.
        /// </param>
        public static void UseDataCoreAdapterDefaults(this JsonSerializerSettings settings) => settings.UseDataCoreAdapterDefaults(null);


        /// <summary>
        /// Registers adapter-related converters and default serialization behaviours.
        /// </summary>
        /// <param name="settings">
        ///   The JSON serialization settings.
        /// </param>
        /// <param name="jsonElementConverterOptions">
        ///   The <see cref="System.Text.Json.JsonSerializerOptions"/> to use with the <see cref="JsonElementConverter"/> 
        ///   registered by this method.
        /// </param>
        public static void UseDataCoreAdapterDefaults(this JsonSerializerSettings settings, System.Text.Json.JsonSerializerOptions? jsonElementConverterOptions) {
            if (settings == null) {
                throw new ArgumentNullException(nameof(settings));
            }

            // Ensure that DateTime values are always serialized as UTC
            settings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            settings.Converters.AddDataCoreAdapterConverters(jsonElementConverterOptions);
        }


        /// <summary>
        /// Registers adapter-related converters.
        /// </summary>
        /// <param name="settings">
        ///   The JSON serialization settings.
        /// </param>
        [Obsolete("This method will be removed in a future version. Call UseDataCoreAdapterDefaults instead.", false)]
        public static void AddDataCoreAdapterConverters(this JsonSerializerSettings settings) => settings.UseDataCoreAdapterDefaults();


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
        [Obsolete("This method will be removed in a future version. Call UseDataCoreAdapterDefaults instead.", false)]
        public static void AddDataCoreAdapterConverters(this JsonSerializerSettings settings, System.Text.Json.JsonSerializerOptions? jsonElementConverterOptions) => settings.UseDataCoreAdapterDefaults(jsonElementConverterOptions);


        /// <summary>
        /// Registers adapter-related converters.
        /// </summary>
        /// <param name="converters">
        ///   The JSON converter collection to add new converters to.
        /// </param>
        public static void AddDataCoreAdapterConverters(this ICollection<JsonConverter> converters) => converters.AddDataCoreAdapterConverters(null);


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
            converters.Add(new IsoDateTimeConverter() { 
                DateTimeStyles = System.Globalization.DateTimeStyles.AdjustToUniversal
            });
        }

    }
}
