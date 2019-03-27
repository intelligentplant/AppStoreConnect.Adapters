using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace DataCore.Adapter.AspNetCore.Authorization {
    public class AdapterApiAuthorizationService {

        private readonly IAuthorizationService _authorizationService;

        public bool UseAuthorization { get; }


        internal AdapterApiAuthorizationService(bool useAuthorization, IAuthorizationService authorizationService) {
            UseAuthorization = useAuthorization;
            if (useAuthorization) {
                _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            }
        }


        internal async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, IAdapter resource) {
            if (!UseAuthorization) {
                return AuthorizationResult.Success();
            }

            return await _authorizationService.AuthorizeAsync(user, resource, new FeatureAuthorizationRequirement(null)).ConfigureAwait(false);
        }


        internal async Task<AuthorizationResult> AuthorizeAsync<TFeature>(ClaimsPrincipal user, IAdapter resource) where TFeature : IAdapterFeature {
            if (!UseAuthorization) {
                return AuthorizationResult.Success();
            }

            return await _authorizationService.AuthorizeAsync(user, resource, new FeatureAuthorizationRequirement<TFeature>()).ConfigureAwait(false);
        }

    }
}
