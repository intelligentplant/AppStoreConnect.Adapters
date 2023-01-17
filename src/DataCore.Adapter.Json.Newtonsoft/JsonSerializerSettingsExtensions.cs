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

            settings.Converters.AddDataCoreAdapterConverters();

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

            converters.Add(new JsonElementConverter());
            converters.Add(new NullableJsonElementConverter());
            converters.Add(new VariantConverter());

        }

    }
}
