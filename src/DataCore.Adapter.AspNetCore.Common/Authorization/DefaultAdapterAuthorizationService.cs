﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace DataCore.Adapter.AspNetCore.Authorization {

    /// <summary>
    /// Provides authorization functionality for adapter API calls.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Authorisation rules are applied using the ASP.NET Core <see cref="IAuthorizationService"/>.
    ///   </para>
    ///   <para>
    ///     To customise the authorisation rules, extend <see cref="FeatureAuthorizationHandler"/> 
    ///     and register it at application startup using the 
    ///     <see cref="Microsoft.Extensions.DependencyInjection.CommonAdapterConfigurationExtensions.AddAdapterFeatureAuthorization{T}"/> 
    ///     extension method.
    ///   </para>
    /// </remarks>
    internal class DefaultAdapterAuthorizationService : IAdapterAuthorizationService {

        /// <summary>
        /// The ASP.NET Core authorization service.
        /// </summary>
        private readonly IAuthorizationService _authorizationService = default!;

        /// <summary>
        /// Indicates if authorization will be applied to adapter API calls. This will be <see langword="false"/> 
        /// unless a <see cref="FeatureAuthorizationHandler"/> is registered using 
        /// <see cref="Microsoft.Extensions.DependencyInjection.CommonAdapterConfigurationExtensions.AddAdapterFeatureAuthorization{T}"/>
        /// during application startup.
        /// </summary>
        public bool UseAuthorization { get; }


        /// <summary>
        /// Creates a new <see cref="DefaultAdapterAuthorizationService"/> object.
        /// </summary>
        /// <param name="useAuthorization">
        ///   Indicates if authorizatio will be applied to adapter API calls.
        /// </param>
        /// <param name="authorizationService">
        ///   The ASP.NET Core authorization service.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="useAuthorization"/> is <see langword="true"/> and <paramref name="authorizationService"/> 
        ///   is <see langword="null"/>.
        /// </exception>
        internal DefaultAdapterAuthorizationService(bool useAuthorization, IAuthorizationService? authorizationService) {
            UseAuthorization = useAuthorization;
            if (useAuthorization) {
                _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            }
        }


        /// <inheritdoc/>
        public async Task<bool> AuthorizeAdapter(IAdapter adapter, IAdapterCallContext context, CancellationToken cancellationToken) {
            using (Diagnostics.Telemetry.ActivitySource.StartActivity("AuthorizeAdapter")) {
                if (!UseAuthorization || context == null) {
                    return true;
                }
                
                var result = await _authorizationService.AuthorizeAsync(context.User!, adapter, new FeatureAuthorizationRequirement(null)).ConfigureAwait(false);
                return result.Succeeded;
            }
        }


        /// <inheritdoc/>
        public async Task<bool> AuthorizeAdapterFeature(IAdapter adapter, IAdapterCallContext context, Uri featureUri, CancellationToken cancellationToken) {
            using (Diagnostics.Telemetry.ActivitySource.StartActivity("AuthorizeAdapterFeature")) {
                if (!UseAuthorization || context == null) {
                    return true;
                }

                var result = await _authorizationService.AuthorizeAsync(context.User!, adapter, new FeatureAuthorizationRequirement(featureUri)).ConfigureAwait(false);
                return result.Succeeded;
            }
        }
    }
}
