using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace DataCore.Adapter.AspNetCore.Authorization {

    /// <summary>
    /// Authorization handler for operations on adapters. See <see cref="AdapterOperations"/> for the 
    /// operations that can be passed to <see cref="AuthorizationHandler{TRequirement, TResource}.HandleRequirementAsync(AuthorizationHandlerContext, TRequirement, TResource)"/>.
    /// </summary>
    public abstract class FeatureAuthorizationHandler : AuthorizationHandler<FeatureAuthorizationRequirement, IAdapter> { }

}
