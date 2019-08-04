using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCore.Adapter {

    /// <summary>
    /// Default <see cref="IAdapterFeaturesCollection"/> implementation.
    /// </summary>
    public class AdapterFeaturesCollection: IAdapterFeaturesCollection, IAsyncDisposable {

        /// <summary>
        /// When <see langword="true"/>, feature implementations that als implement <see cref="IDisposable"/> 
        /// or <see cref="IAsyncDisposable"/> will be disposed when the collection is disposed.
        /// </summary>
        private bool _disposeFeatures;

        /// <summary>
        /// The implemented features.
        /// </summary>
        private readonly ConcurrentDictionary<Type, object> _features = new ConcurrentDictionary<Type, object>();


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
        /// <param name="disposeFeatures">
        ///   When <see langword="true"/>, feature implementations that als implement <see cref="IDisposable"/> 
        ///   or <see cref="IAsyncDisposable"/> will be disposed when the collection is disposed.
        /// </param>
        public AdapterFeaturesCollection(bool disposeFeatures = false) {
            _disposeFeatures = disposeFeatures;
        }


        /// <summary>
        /// Creates a new <see cref="AdapterFeaturesCollection"/> object that is pre-populated using 
        /// the specified object.
        /// </summary>
        /// <param name="featureProvider">
        ///   The object that will provide the adapter feature implementations.
        /// </param>
        /// <param name="disposeFeatures">
        ///   When <see langword="true"/>, feature implementations that als implement <see cref="IDisposable"/> 
        ///   or <see cref="IAsyncDisposable"/> will be disposed when the collection is disposed.
        /// </param>
        /// <remarks>
        ///   All interfaces implemented by the <paramref name="featureProvider"/> that extend 
        ///   <see cref="IAdapterFeature"/> will be registered with the <see cref="AdapterFeaturesCollection"/>.
        /// </remarks>
        public AdapterFeaturesCollection(object featureProvider, bool disposeFeatures = false) : this(disposeFeatures) {
            AddFromProvider(featureProvider);
        }


        /// <summary>
        /// Adds all adapter features implemented by the specified feature provider.
        /// </summary>
        /// <param name="featureProvider">
        ///   The object that will provide the adapter feature implementations.
        /// </param>
        /// <param name="addStandardFeatures">
        ///   Specifies if standard adapter feature implementations should be added to the 
        ///   collection. Standard feature types can be obtained by calling 
        ///   <see cref="TypeExtensions.GetStandardAdapterFeatureTypes"/>.
        /// </param>
        /// <param name="addExtensionFeatures">
        ///   Specifies if extension adapter feature implementations should be added to the 
        ///   collection. Extension features must derive from <see cref="IAdapterExtensionFeature"/>.
        /// </param>
        /// <remarks>
        ///   All interfaces implemented by the <paramref name="featureProvider"/> that extend 
        ///   <see cref="IAdapterFeature"/> will be registered with the 
        ///   <see cref="AdapterFeaturesCollection"/> (assuming that they meet the 
        ///   <paramref name="addStandardFeatures"/> and <paramref name="addExtensionFeatures"/> 
        ///   constraints).
        /// </remarks>
        public void AddFromProvider(object featureProvider, bool addStandardFeatures = true, bool addExtensionFeatures = true) {
            if (featureProvider == null) {
                return;
            }

            var implementedFeatures = featureProvider.GetType().GetInterfaces().Where(x => x.IsAdapterFeature());
            foreach (var feature in implementedFeatures) {
                if (!addStandardFeatures && feature.IsStandardAdapterFeature()) {
                    continue;
                }
                if (!addExtensionFeatures && feature.IsExtensionAdapterFeature()) {
                    continue;
                }
                AddInternal(feature, featureProvider, false);
            }
        }


        /// <summary>
        /// Adds an adapter feature.
        /// </summary>
        /// <param name="featureType">
        ///   The feature interface type.
        /// </param>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        /// <param name="throwOnAlreadyAdded">
        ///   Flags if an exception should be thrown if the feature type has already been registered.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="feature"/> is not an instance of <paramref name="featureType"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <paramref name="featureType"/> has already been registered and 
        ///   <paramref name="throwOnAlreadyAdded"/> is <see langword="true"/>.
        /// </exception>
        private void AddInternal(Type featureType, object feature, bool throwOnAlreadyAdded) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }
            if (!featureType.IsInstanceOfType(feature)) {
                throw new ArgumentException(string.Format(Resources.Error_NotAFeatureImplementation, featureType.FullName), nameof(feature));
            }
            if (!_features.TryAdd(featureType, feature)) {
                if (throwOnAlreadyAdded) {
                    throw new ArgumentException(Resources.Error_FeatureIsAlreadyRegistered, nameof(featureType));
                }
            }
        }


        /// <summary>
        /// Adds an adapter feature.
        /// </summary>
        /// <param name="featureType">
        ///   The feature interface type.
        /// </param>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="featureType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="featureType"/> is not an adapter feature type.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="feature"/> is not an instance of <paramref name="featureType"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <paramref name="featureType"/> has already been registered.
        /// </exception>
        public void Add(Type featureType, object feature) {
            if (featureType == null) {
                throw new ArgumentNullException(nameof(featureType));
            }
            if (!featureType.IsAdapterFeature()) {
                throw new ArgumentException(string.Format(SharedResources.Error_NotAnAdapterFeature, nameof(IAdapterFeature), nameof(IAdapterExtensionFeature)), nameof(featureType));
            }

            AddInternal(featureType, feature, true);
        }


        /// <summary>
        /// Adds an adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The feature. This must be an interface derived from <see cref="IAdapterFeature"/>.
        /// </typeparam>
        /// <typeparam name="TFeatureImpl">
        ///   The feature implementation type. This must be a concrete class that implements 
        ///   <typeparamref name="TFeature"/>.
        /// </typeparam>
        /// <param name="feature">
        ///   The feature implementation.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFeature"/> is not an interface, or it does not interit from 
        ///   <see cref="IAdapterFeature"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="feature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An implementation of <typeparamref name="TFeature"/> has already been registered.
        /// </exception>
        public void Add<TFeature, TFeatureImpl>(TFeatureImpl feature) where TFeature : IAdapterFeature where TFeatureImpl : class, TFeature {
            if (!typeof(TFeature).IsAdapterFeature()) {
                throw new ArgumentException(string.Format(SharedResources.Error_NotAnAdapterFeature, nameof(IAdapterFeature), nameof(IAdapterExtensionFeature)), nameof(feature));
            }
            AddInternal(typeof(TFeature), feature, true);
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
        public bool Remove<TFeature>() where TFeature : IAdapterFeature {
            return _features.TryRemove(typeof(TFeature), out var _);
        }


        /// <summary>
        /// Removes all adapter features.
        /// </summary>
        public void Clear() {
            _features.Clear();
        }


        /// <summary>
        /// Asynchronously disposes of any features in the collection that implement 
        /// <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>.
        /// </summary>
        /// <returns>
        ///   A task that will dispose of the collection.
        /// </returns>
        public async ValueTask DisposeAsync() {
            if (!_disposeFeatures) {
                _features.Clear();
                return;
            }

            var features = _features.Values.ToArray();
            _features.Clear();

            foreach (var item in features) {
                if (item is IAsyncDisposable ad) {
                    await ad.DisposeAsync().ConfigureAwait(false);
                }
                else if (item is IDisposable d) {
                    d.Dispose();
                }
            }
        }

    }
}
