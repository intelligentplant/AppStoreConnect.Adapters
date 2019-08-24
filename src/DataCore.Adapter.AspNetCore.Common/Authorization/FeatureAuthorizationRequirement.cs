using System;
using Microsoft.AspNetCore.Authorization;

namespace DataCore.Adapter.AspNetCore.Authorization {

    /// <summary>
    /// A helper class to provide an <see cref="IAuthorizationRequirement"/> containing an adapter 
    /// feature.
    /// </summary>
    public class FeatureAuthorizationRequirement : IAuthorizationRequirement {

        /// <summary>
        /// The adapter feature to authorize. Can be <see langword="null"/> if the requirement is that 
        /// the adapter is visible to the caller.
        /// </summary>
        public Type Feature { get; }
        

        /// <summary>
        /// Creates a new <see cref="FeatureAuthorizationRequirement"/> object.
        /// </summary>
        /// <param name="feature">
        ///   The feature type.
        /// </param>
        internal FeatureAuthorizationRequirement(Type feature) {
            Feature = feature;
        }

    }


    /// <summary>
    /// A helper class to provide an <see cref="IAuthorizationRequirement"/> containing an adapter 
    /// feature.
    /// </summary>
    /// <typeparam name="TFeature">
    ///   The feature type.
    /// </typeparam>
    public class FeatureAuthorizationRequirement<TFeature> : FeatureAuthorizationRequirement where TFeature : IAdapterFeature {

        /// <summary>
        /// Creates a new <see cref="FeatureAuthorizationRequirement{TFeature}"/> object.
        /// </summary>
        public FeatureAuthorizationRequirement() : base(typeof(TFeature)) { }

    }
}
