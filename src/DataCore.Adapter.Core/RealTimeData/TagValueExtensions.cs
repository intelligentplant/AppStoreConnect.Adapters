﻿using System;
using System.Collections.Generic;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extension methods for <see cref="TagValue"/>.
    /// </summary>
    public static class TagValueExtensions {

        /// <summary>
        /// Tests if the <see cref="TagValue.Value"/> of a <see cref="TagValue"/> has a numeric type.
        /// </summary>
        /// <param name="value">
        ///   The <see cref="TagValue"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="TagValue.Value"/> has a numeric type, or <see langword="false"/> 
        ///   otherwise.
        /// </returns>
        /// <remarks>
        /// 
        /// The following value types are considered to be numeric:
        /// 
        /// <list type="bullet">
        ///   <item>
        ///     <description><see cref="VariantType.Boolean"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Byte"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Double"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Float"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int16"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int32"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.Int64"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.SByte"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt16"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt32"/></description>
        ///   </item>
        ///   <item>
        ///     <description><see cref="VariantType.UInt64"/></description>
        ///   </item>
        /// </list>
        /// 
        /// All other types are considered to be non-numeric.
        /// 
        /// </remarks>
        public static bool IsNumericType(this TagValue value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            return value.Value.IsNumericType();
        }


        /// <summary>
        /// Gets the first value in the <see cref="TagValue"/> that can be cast to the specified 
        /// type, or returns a default value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the value to return.
        /// </typeparam>
        /// <param name="value">
        ///   The <see cref="TagValue"/>.
        /// </param>
        /// <returns>
        ///   The first tag value that can be cast to an instance of <typeparamref name="T"/>, 
        ///   or the provided default value if the no such conversion can be performed on any of 
        ///   the values in the <see cref="TagValue"/>.
        /// </returns>
        public static T? GetValueOrDefault<T>(this TagValue value) {
            return value.GetValueOrDefault(default(T));
        }


        /// <summary>
        /// Gets the first value in the <see cref="TagValue"/> that can be cast to the specified 
        /// type, or returns a default value.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of the value to return.
        /// </typeparam>
        /// <param name="value">
        ///   The <see cref="TagValue"/>.
        /// </param>
        /// <param name="defaultValue">
        ///   The default value to return if none of the values can be converted to 
        ///   <typeparamref name="T"/>.
        /// </param>
        /// <returns>
        ///   The first tag value that can be cast to an instance of <typeparamref name="T"/>, 
        ///   or the provided default value if the no such conversion can be performed on any of 
        ///   the values in the <see cref="TagValue"/>.
        /// </returns>
        public static T? GetValueOrDefault<T>(this TagValue value, T? defaultValue) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }

            return value.Value.GetValueOrDefault(defaultValue);
        }

    }
}
