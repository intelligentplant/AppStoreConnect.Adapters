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
        public Uri? FeatureUri { get; }
        

        /// <summary>
        /// Creates a new <see cref="FeatureAuthorizationRequirement"/> object.
        /// </summary>
        /// <param name="feature">
        ///   The feature type.
        /// </param>
        internal FeatureAuthorizationRequirement(Uri? feature) {
            FeatureUri = feature;
        }

    }

}
