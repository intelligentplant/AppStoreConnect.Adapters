using System;

namespace DataCore.Adapter {

    /// <summary>
    /// Extensions for <see cref="IAdapterFeature"/> and derived types.
    /// </summary>
    public static class AdapterFeatureExtensions {

        /// <summary>
        /// Unwraps the specified adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The adapter feature type.
        /// </typeparam>
        /// <param name="feature">
        ///   The adapter feature.
        /// </param>
        /// <returns>
        ///   The unwrapped feature.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Adapter features registered with a class derived from <see cref="AdapterCore"/> are 
        ///   wrapped inside instances of <see cref="AdapterFeatureWrapper{TFeature}"/>, to provide 
        ///   automatic validation of feature invocations and to allow for the generation of 
        ///   telemetry.
        /// </para>
        /// 
        /// <para>
        ///   Calling <see cref="Unwrap"/> on a wrapped feature will return the original feature 
        ///   implementation that was registered with the adapter. If the <paramref name="feature"/> 
        ///   is not an instance of <see cref="AdapterFeatureWrapper{TFeature}"/>, the return value 
        ///   will be the <paramref name="feature"/>.
        /// </para>
        /// 
        /// </remarks>
        public static TFeature Unwrap<TFeature>(this TFeature feature) where TFeature : IAdapterFeature {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            if (feature is AdapterFeatureWrapper<TFeature> wrapper) {
                return wrapper.InnerFeature;
            }

            return feature;
        }

    }
}
