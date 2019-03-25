using System;
using System.Collections.Generic;
using System.Text;

namespace DataCore.Adapter {

    /// <summary>
    /// Default <see cref="IAdapterFeaturesCollection"/> implementation. Use <see cref="Add{TFeature}(TFeature)"/> 
    /// and <see cref="Remove{TFeature}"/> to add and remove features.
    /// </summary>
    public class AdapterFeaturesCollection: IAdapterFeaturesCollection {

        /// <summary>
        /// The implemented features.
        /// </summary>
        private readonly IDictionary<Type, object> _features = new Dictionary<Type, object>();


        /// <inheritdoc/>
        public IEnumerable<Type> Keys {
            get { return _features.Keys; }
        }


        /// <inheritdoc/>
        public object this[Type key] {
            get {
                return key == null || !_features.TryGetValue(key, out var value)
                    ? null
                    : value;
            }
        }


        /// <inheritdoc/>
        public TFeature Get<TFeature>() where TFeature : IAdapterFeature {
            var type = typeof(TFeature);
            return (TFeature) this[type];
        }


        /// <summary>
        /// Adds an adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not an interface, or it does not interit from 
        ///   <see cref="IAdapterFeature"/>.
        /// </exception>
        public void Add<TFeature>(TFeature feature) where TFeature : class, IAdapterFeature {
            var rootFeatureType = typeof(IAdapterFeature);
            var featureType = typeof(TFeature);
            if (!featureType.IsInterface || !rootFeatureType.IsAssignableFrom(featureType)) {
                throw new ArgumentException(string.Format(Resources.Error_NotAnAdapterFeature, nameof(IAdapterFeature)), nameof(feature));
            }

            _features[typeof(TFeature)] = feature;
        }


        /// <summary>
        /// Removes an adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature type.
        /// </typeparam>
        /// <returns>
        ///   <see langword="true"/> if the feature was removed, or <see langword="false"/> otherwise.
        /// </returns>
        public bool Remove<TFeature>() where TFeature : class, IAdapterFeature {
            return _features.Remove(typeof(TFeature));
        }

    }
}
