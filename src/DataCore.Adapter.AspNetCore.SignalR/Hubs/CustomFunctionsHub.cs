﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.AspNetCore.Hubs {
    partial class AdapterHub {

        /// <summary>
        /// Gets the custom functions defined by the specified adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   The custom function definitions.
        /// </returns>
        public async Task<IEnumerable<CustomFunctionDescriptor>> GetCustomFunctions(
            string adapterId,
            GetCustomFunctionsRequest request
        ) {
            if (adapterId == null) {
                throw new ArgumentNullException(nameof(adapterId));
            }
            ValidateObject(request);

            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);

            var resolvedFeature = await ResolveAdapterAndFeature<ICustomFunctions>(
                adapterCallContext,
                adapterId,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            return await resolvedFeature.Feature.GetFunctionsAsync(adapterCallContext, request, Context.ConnectionAborted).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets the extended definiton for the specified custom function.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The request.
        /// </param>
        /// <returns>
        ///   The extended custom function definition.
        /// </returns>
        public async Task<CustomFunctionDescriptorExtended?> GetCustomFunction(
            string adapterId,
            GetCustomFunctionRequest request
        ) {
            if (adapterId == null) {
                throw new ArgumentNullException(nameof(adapterId));
            }
            ValidateObject(request);

            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);

            var resolvedFeature = await ResolveAdapterAndFeature<ICustomFunctions>(
                adapterCallContext,
                adapterId,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            return await resolvedFeature.Feature.GetFunctionAsync(adapterCallContext, request, Context.ConnectionAborted).ConfigureAwait(false);
        }


        /// <summary>
        /// Invokes a custom function on an adapter.
        /// </summary>
        /// <param name="adapterId">
        ///   The adapter ID.
        /// </param>
        /// <param name="request">
        ///   The invocation request.
        /// </param>
        /// <returns>
        ///   The invocation response.
        /// </returns>
        public async Task<CustomFunctionInvocationResponse> InvokeCustomFunction(
            string adapterId,
            CustomFunctionInvocationRequest request
        ) {
            if (adapterId == null) {
                throw new ArgumentNullException(nameof(adapterId));
            }
            ValidateObject(request);

            var adapterCallContext = new SignalRAdapterCallContext(Context, _serviceProvider);

            var resolvedFeature = await ResolveAdapterAndFeature<ICustomFunctions>(
                adapterCallContext,
                adapterId,
                Context.ConnectionAborted
            ).ConfigureAwait(false);

            var function = await resolvedFeature.Feature.GetFunctionAsync(adapterCallContext, new GetCustomFunctionRequest() {
                Id = request.Id,
            }, Context.ConnectionAborted).ConfigureAwait(false);

            if (function == null) {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, AbstractionsResources.Error_UnableToResolveCustomFunction, request.Id), nameof(request));
            }

            if (!request.TryValidateBody(function, _jsonOptions, out var validationResults)) {
                throw new System.ComponentModel.DataAnnotations.ValidationException(System.Text.Json.JsonSerializer.Serialize(validationResults));
            }

            return await resolvedFeature.Feature.InvokeFunctionAsync(adapterCallContext, request, Context.ConnectionAborted).ConfigureAwait(false);
        }

    }
}
