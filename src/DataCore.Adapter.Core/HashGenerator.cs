#if !NETSTANDARD2_1_OR_GREATER

namespace DataCore.Adapter {

    /// <summary>
    /// Assists with hash code generation.
    /// </summary>
    /// <remarks>
    ///   Hash code implementation is from https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-overriding-gethashcode/263416
    /// </remarks>
    public static class HashGenerator {

        /// <summary>
        /// Create a hash code from the specified parameter.
        /// </summary>
        /// <typeparam name="T1">
        ///   The parameter type.
        /// </typeparam>
        /// <param name="val">
        ///   The value.
        /// </param>
        /// <returns>
        ///   The hash code.
        /// </returns>
        public static int Combine<T1>(T1 val) {
            unchecked {
                const int HashingBase = (int) 2166136261;
                const int HashingMultiplier = 16777619;

                var hash = HashingBase;
                hash = (hash * HashingMultiplier) ^ val?.GetHashCode() ?? 0;

                return hash;
            }
        }


        /// <summary>
        /// Create a hash code from the specified parameters.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <param name="val1">
        ///   The first value.
        /// </param>
        /// <param name="val2">
        ///   The second value.
        /// </param>
        /// <returns>
        ///   The hash code.
        /// </returns>
        public static int Combine<T1, T2>(T1 val1, T2 val2) {
            unchecked {
                const int HashingBase = (int) 2166136261;
                const int HashingMultiplier = 16777619;

                var hash = HashingBase;
                hash = (hash * HashingMultiplier) ^ val1?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val2?.GetHashCode() ?? 0;

                return hash;
            }
        }


        /// <summary>
        /// Create a hash code from the specified parameters.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <param name="val1">
        ///   The first value.
        /// </param>
        /// <param name="val2">
        ///   The second value.
        /// </param>
        /// <param name="val3">
        ///   The third value.
        /// </param>
        /// <returns>
        ///   The hash code.
        /// </returns>
        public static int Combine<T1, T2, T3>(T1 val1, T2 val2, T3 val3) {
            unchecked {
                const int HashingBase = (int) 2166136261;
                const int HashingMultiplier = 16777619;

                var hash = HashingBase;
                hash = (hash * HashingMultiplier) ^ val1?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val2?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val3?.GetHashCode() ?? 0;

                return hash;
            }
        }


        /// <summary>
        /// Create a hash code from the specified parameters.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <param name="val1">
        ///   The first value.
        /// </param>
        /// <param name="val2">
        ///   The second value.
        /// </param>
        /// <param name="val3">
        ///   The third value.
        /// </param>
        /// <param name="val4">
        ///   The fourth value.
        /// </param>
        /// <returns>
        ///   The hash code.
        /// </returns>
        public static int Combine<T1, T2, T3, T4>(T1 val1, T2 val2, T3 val3, T4 val4) {
            unchecked {
                const int HashingBase = (int) 2166136261;
                const int HashingMultiplier = 16777619;

                var hash = HashingBase;
                hash = (hash * HashingMultiplier) ^ val1?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val2?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val3?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val4?.GetHashCode() ?? 0;

                return hash;
            }
        }


        /// <summary>
        /// Create a hash code from the specified parameters.
        /// </summary>
        /// <typeparam name="T1">
        ///   The first parameter type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///   The second parameter type.
        /// </typeparam>
        /// <typeparam name="T3">
        ///   The third parameter type.
        /// </typeparam>
        /// <typeparam name="T4">
        ///   The fourth parameter type.
        /// </typeparam>
        /// <typeparam name="T5">
        ///   The fifth parameter type.
        /// </typeparam>
        /// <param name="val1">
        ///   The first value.
        /// </param>
        /// <param name="val2">
        ///   The second value.
        /// </param>
        /// <param name="val3">
        ///   The third value.
        /// </param>
        /// <param name="val4">
        ///   The fourth value.
        /// </param>
        /// <param name="val5">
        ///   The fifth value.
        /// </param>
        /// <returns>
        ///   The hash code.
        /// </returns>
        public static int Combine<T1, T2, T3, T4, T5>(T1 val1, T2 val2, T3 val3, T4 val4, T5 val5) {
            unchecked {
                const int HashingBase = (int) 2166136261;
                const int HashingMultiplier = 16777619;

                var hash = HashingBase;
                hash = (hash * HashingMultiplier) ^ val1?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val2?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val3?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val4?.GetHashCode() ?? 0;
                hash = (hash * HashingMultiplier) ^ val5?.GetHashCode() ?? 0;

                return hash;
            }
        }

    }

}

#endif
