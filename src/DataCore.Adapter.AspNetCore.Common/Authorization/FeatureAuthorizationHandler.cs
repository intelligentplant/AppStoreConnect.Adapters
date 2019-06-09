using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DataCore.Adapter.AspNetCore.Authorization {

    /// <summary>
    /// Authorization handler for operations on adapters.
    /// </summary>
    /// <seealso cref="FeatureAuthorizationRequirement"/>
    public abstract class FeatureAuthorizationHandler : AuthorizationHandler<FeatureAuthorizationRequirement, IAdapter> { }

}
