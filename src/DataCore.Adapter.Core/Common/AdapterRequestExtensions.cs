using System;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Extensions for <see cref="AdapterRequest"/> instances.
    /// </summary>
    public static class AdapterRequestExtensions {

        /// <summary>
        /// Tries to retrieve a property value from the request's <see cref="AdapterRequest.Properties"/> 
        /// collection and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to convert the property value to.
        /// </typeparam>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="key">
        ///   The property name.
        /// </param>
        /// <param name="value">
        ///   The converted property value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the property could be successfully converted to 
        ///   <typeparamref name="T"/>, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryGetProperty<T>(this AdapterRequest request, string key, out T value) where T : IConvertible {
            return request.TryGetProperty(key, null, out value);
        }


        /// <summary>
        /// Tries to retrieve a property value from the request's <see cref="AdapterRequest.Properties"/> 
        /// collection and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///   The type to convert the property value to.
        /// </typeparam>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <param name="key">
        ///   The property name.
        /// </param>
        /// <param name="formatProvider">
        ///   The <see cref="IFormatProvider"/> to use when converting the property value to an 
        ///   instance of <typeparamref name="T"/>. Can be <see langword="null"/>.
        /// </param>
        /// <param name="value">
        ///   The converted property value.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the property could be successfully converted to 
        ///   <typeparamref name="T"/>, or <see langword="false"/> otherwise.
        /// </returns>
        public static bool TryGetProperty<T>(this AdapterRequest request, string key, IFormatProvider formatProvider, out T value) where T : IConvertible {
            if (request?.Properties == null || key == null || !request.Properties.TryGetValue(key, out var val) || val == null) {
                value = default;
                return false;
            }

            try {
                value = (T) Convert.ChangeType(val, typeof(T), formatProvider);
                return true;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch {
#pragma warning restore CA1031 // Do not catch general exception types
                value = default;
                return false;
            }
        }

    }
}
