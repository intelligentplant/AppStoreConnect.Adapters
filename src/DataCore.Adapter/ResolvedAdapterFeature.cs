using System;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter {

    /// <summary>
    /// Describes the result of a request to resolve and authorize an adapter feature.
    /// </summary>
    /// <typeparam name="TFeature">
    ///   The adapter feature type.
    /// </typeparam>
    public struct ResolvedAdapterFeature<TFeature> : IEquatable<ResolvedAdapterFeature<TFeature>> where TFeature : IAdapterFeature {

        /// <summary>
        /// The resolved feature.
        /// </summary>
        private TFeature _feature;

        /// <summary>
        /// The adapter. The value will be <see langword="null"/> if the adapter could not be resolved.
        /// </summary>
        public IAdapter Adapter { get; private set; }

        /// <summary>
        /// The feature. The value will be <see langword="null"/> if the adapter or feature could not 
        /// be resolved.
        /// </summary>
        public TFeature Feature => IsFeatureResolved ? _feature : default;

        /// <summary>
        /// <see langword="true"/> if the adapter was resolved, or <see langword="false"/> otherwise.
        /// </summary>
        public bool IsAdapterResolved => Adapter != null;

        /// <summary>
        /// <see langword="true"/> if the feature was resolved, or <see langword="false"/> otherwise.
        /// </summary>
        public bool IsFeatureResolved => _feature != null;

        /// <summary>
        /// <see langword="true"/> if access to the feature was authorized, or <see langword="false"/> 
        /// otherwise.
        /// </summary>
        public bool IsFeatureAuthorized { get; private set; }

        /// <summary>
        /// <see langword="true"/> if the feature is an extension feature, or <see langword="false"/>
        /// if it is a standard feature.
        /// </summary>
        public bool IsExtensionFeature => _feature is IAdapterExtensionFeature;


        /// <summary>
        /// Creates a new <see cref="ResolvedAdapterFeature{TFeature}"/> object.
        /// </summary>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <param name="feature">
        ///   The feature.
        /// </param>
        /// <param name="isFeatureAuthorized">
        ///   A flag indicating if access to the feature was authorized.
        /// </param>
        public ResolvedAdapterFeature(IAdapter adapter, TFeature feature, bool isFeatureAuthorized) {
            Adapter = adapter;
            _feature = feature;
            IsFeatureAuthorized = isFeatureAuthorized;
        }


        /// <inheritdoc/>
        public override int GetHashCode() {
#if NETSTANDARD2_0
            return HashGenerator.Combine(Adapter, _feature, IsFeatureAuthorized);
#else
            return HashCode.Combine(Adapter, _feature, IsFeatureAuthorized);
#endif
        }


        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is ResolvedAdapterFeature<TFeature> resolvedFeature)) {
                return false;
            }

            return Equals(resolvedFeature);
        }


        /// <inheritdoc/>
        public bool Equals(ResolvedAdapterFeature<TFeature> other) {
            return Equals(Adapter, other.Adapter) &&
                Equals(_feature, other._feature) &&
                IsFeatureAuthorized == other.IsFeatureAuthorized;
        }


        /// <inheritdoc/>
        public static bool operator ==(ResolvedAdapterFeature<TFeature> left, ResolvedAdapterFeature<TFeature> right) {
            return left.Equals(right);
        }


        /// <inheritdoc/>
        public static bool operator !=(ResolvedAdapterFeature<TFeature> left, ResolvedAdapterFeature<TFeature> right) {
            return !(left == right);
        }

    }
}
