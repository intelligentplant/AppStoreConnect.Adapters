using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace DataCore.Adapter.AspNetCore.Authorization {
    public class FeatureAuthorizationRequirement : IAuthorizationRequirement {

        /// <summary>
        /// The adapter feature to authorize. Can be <see langword="null"/> if the requirement is that 
        /// the adapter is visible to the caller.
        /// </summary>
        public Type Feature { get; }

        internal FeatureAuthorizationRequirement(Type feature) {
            Feature = feature;
        }

    }


    public class FeatureAuthorizationRequirement<TFeature> : FeatureAuthorizationRequirement where TFeature : IAdapterFeature {

        public FeatureAuthorizationRequirement() : base(typeof(TFeature)) { }

    }
}
