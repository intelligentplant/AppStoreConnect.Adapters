using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace DataCore.Adapter.AspNetCore.Authorization {

    /// <summary>
    /// Provides authorization functionality for adapter API calls. To customise the authorization 
    /// rules, create a subclass of the <see cref="FeatureAuthorizationHandler"/> and register it 
    /// using <see cref="AdapterServicesOptionsBuilder.UseFeatureAuthorizationHandler{THandler}"/>
    /// during application startup.
    /// </summary>
    public class AdapterApiAuthorizationService {

        /// <summary>
        /// The ASP.NET Core authorization service.
        /// </summary>
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Indicates if authorization will be applied to adapter API calls. This will be <see langword="false"/> 
        /// unless a <see cref="FeatureAuthorizationHandler"/> is registered using <see cref="AdapterServicesOptionsBuilder.UseFeatureAuthorizationHandler{THandler}"/>
        /// during application startup.
        /// </summary>
        public bool UseAuthorization { get; }


        /// <summary>
        /// Creates a new <see cref="AdapterApiAuthorizationService"/> object.
        /// </summary>
        /// <param name="useAuthorization">
        ///   Indicates if authorizatio will be applied to adapter API calls.
        /// </param>
        /// <param name="authorizationService">
        ///   The ASP.NET Core authorization service.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="authorizationService"/> is <see langword="null"/>.
        /// </exception>
        internal AdapterApiAuthorizationService(bool useAuthorization, IAuthorizationService authorizationService) {
            UseAuthorization = useAuthorization;
            if (useAuthorization) {
                _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            }
        }


        /// <summary>
        /// Authorizes access to an adapter.
        /// </summary>
        /// <param name="user">
        ///   The calling user.
        /// </param>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The authorization result.
        /// </returns>
        internal async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, IAdapter adapter) {
            if (!UseAuthorization) {
                return AuthorizationResult.Success();
            }

            return await _authorizationService.AuthorizeAsync(user, adapter, new FeatureAuthorizationRequirement(null)).ConfigureAwait(false);
        }


        /// <summary>
        /// Authorizes access to an adapter feature.
        /// </summary>
        /// <typeparam name="TFeature">
        ///   The adapter feature type.
        /// </typeparam>
        /// <param name="user">
        ///   The calling user.
        /// </param>
        /// <param name="adapter">
        ///   The adapter.
        /// </param>
        /// <returns>
        ///   The authorization result.
        /// </returns>
        internal async Task<AuthorizationResult> AuthorizeAsync<TFeature>(ClaimsPrincipal user, IAdapter adapter) where TFeature : IAdapterFeature {
            if (!UseAuthorization) {
                return AuthorizationResult.Success();
            }

            return await _authorizationService.AuthorizeAsync(user, adapter, new FeatureAuthorizationRequirement<TFeature>()).ConfigureAwait(false);
        }

    }
}
