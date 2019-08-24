using Microsoft.AspNetCore.Authorization;

namespace DataCore.Adapter.AspNetCore.Authorization {

    /// <summary>
    /// Authorization handler for operations on adapters.
    /// </summary>
    /// <seealso cref="FeatureAuthorizationRequirement"/>
    public abstract class FeatureAuthorizationHandler : AuthorizationHandler<FeatureAuthorizationRequirement, IAdapter> { }

}
