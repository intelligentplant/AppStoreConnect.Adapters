using System;
using System.Collections.Generic;
using System.Linq;
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


        /// <summary>
        /// Creates a new <see cref="AdapterFeaturesCollection"/> object.
        /// </summary>
        public AdapterFeaturesCollection() { }


        /// <summary>
        /// Creates a new <see cref="AdapterFeaturesCollection"/> object that is pre-populated using 
        /// the specified object.
        /// </summary>
        /// <param name="featureProvider">
        ///   The object that will provide the adapter feature implementations.
        /// </param>
        public AdapterFeaturesCollection(object featureProvider) : this() {
            if (featureProvider == null) {
                return;
            }

            var rootFeatureType = typeof(IAdapterFeature);
            var implementedFeatures = featureProvider.GetType().GetInterfaces().Where(t => rootFeatureType.IsAssignableFrom(t));
            foreach (var feature in implementedFeatures) {
                _features[feature] = featureProvider;
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
                throw new ArgumentException(string.Format(SharedResources.Error_NotAnAdapterFeature, nameof(IAdapterFeature)), nameof(feature));
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
