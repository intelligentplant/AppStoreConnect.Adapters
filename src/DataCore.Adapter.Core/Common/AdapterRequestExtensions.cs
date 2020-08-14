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
        /// <remarks>
        ///   If <typeparamref name="T"/> implements <see cref="IConvertible"/>, <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> 
        ///   will be used to perform the conversion. Support is also provided for <see cref="TimeSpan"/> 
        ///   and <see cref="Uri"/>. Conversion will fail for any other type.
        /// </remarks>
        public static bool TryGetProperty<T>(this AdapterRequest request, string key, out T value) {
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
        /// <remarks>
        ///   If <typeparamref name="T"/> implements <see cref="IConvertible"/>, <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> 
        ///   will be used to perform the conversion. Support is also provided for <see cref="TimeSpan"/> 
        ///   and <see cref="Uri"/>. Conversion will fail for any other type.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "TryGet pattern")]
        public static bool TryGetProperty<T>(this AdapterRequest request, string key, IFormatProvider formatProvider, out T value) {
            if (request?.Properties == null || key == null || !request.Properties.TryGetValue(key, out var val)) {
                value = default;
                return false;
            }

            var t = typeof(T);
            object result;

            if (t == typeof(string)) {
                result = val;
                value = (T) result;
                return true;
            }

            if (val == null) {
                value = default;
                return false;
            }

            if (typeof(IConvertible).IsAssignableFrom(t)) {
                try {
                    value = (T) Convert.ChangeType(val, t, formatProvider);
                    return true;
                }
                catch {
                    value = default;
                    return false;
                }
            }

            if (t == typeof(TimeSpan) && TimeSpan.TryParse(val, formatProvider, out var ts)) {
                result = ts;
                value = (T) result;
                return true;
            }

            if (t == typeof(Uri) && Uri.TryCreate(val, UriKind.Absolute, out var uri)) {
                result = uri;
                value = (T) result;
                return true;
            }

            value = default;
            return false;
        }

    }
}
