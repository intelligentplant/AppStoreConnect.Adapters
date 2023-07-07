using Microsoft.AspNetCore.Http;

using MiniValidation;

namespace DataCore.Adapter.AspNetCore.Internal {
    internal static class Utils {

        /// <summary>
        /// Converts the specified <see cref="DateTime"/> to UTC if it is not alreadt a UTC 
        /// timestamp.
        /// </summary>
        /// <param name="dt">
        ///   The <see cref="DateTime"/> instance.
        /// </param>
        /// <returns>
        ///   The equivalent UTC <see cref="DateTime"/> instance.
        /// </returns>
        /// <remarks>
        ///   If the <see cref="DateTime.Kind"/> for the <paramref name="dt"/> parameter is 
        ///   <see cref="DateTimeKind.Unspecified"/>, it will be treated as if it is UTC. A 
        ///   <see cref="DateTime"/> instance with the same <see cref="DateTime.Ticks"/> value but 
        ///   with a <see cref="DateTime.Kind"/> value of <see cref="DateTimeKind.Utc"/> will be 
        ///   returned.
        /// </remarks>
        internal static DateTime ConvertToUniversalTime(DateTime dt) {
            if (dt.Kind == DateTimeKind.Utc) {
                return dt;
            }

            if (dt.Kind == DateTimeKind.Local) {
                return dt.ToUniversalTime();
            }

            // Unspecified kind; assume that it is actually UTC.
            return new DateTime(dt.Ticks, DateTimeKind.Utc);
        }


        private static ResolvedAdapter<TFeature> CreateErrorResult<TFeature>(IResult error) where TFeature : IAdapterFeature {
            return new ResolvedAdapter<TFeature>() {
                CallContext = default!,
                Adapter = default!,
                Feature = default!,
                Error = error,
            };
        }


        private static ResolvedAdapter CreateErrorResult(IResult error) {
            return new ResolvedAdapter() {
                CallContext = default!,
                Adapter = default!,
                Error = error,
            };
        }


        internal static async ValueTask<IResult?> ValidateRequestAsync(object request, bool recursive = true) {
            var resolverResult = await MiniValidator.TryValidateAsync(request, recursive).ConfigureAwait(false);
            if (!resolverResult.IsValid) {
                return Results.ValidationProblem(resolverResult.Errors);
            }

            return null;
        }


        internal static IResult CreateAdapterNotFoundResult(IAdapterCallContext callContext, string adapterId) => Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId));


        internal static async ValueTask<ResolvedAdapter<TFeature>> ResolveAdapterAsync<TFeature>(
            HttpContext context,
            IAdapterAccessor accessor,
            string adapterId,
            CancellationToken cancellationToken
        ) where TFeature : IAdapterFeature {
            var callContext = new HttpAdapterCallContext(context);

            var resolvedFeature = await accessor.GetAdapterAndFeature<TFeature>(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return CreateErrorResult<TFeature>(CreateAdapterNotFoundResult(callContext, adapterId));
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return CreateErrorResult<TFeature>(Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)));
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return CreateErrorResult<TFeature>(Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, typeof(TFeature).Name)));
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return CreateErrorResult<TFeature>(Results.Problem(statusCode: StatusCodes.Status403Forbidden));
            }

            return new ResolvedAdapter<TFeature>() {
                CallContext = callContext,
                Adapter = resolvedFeature.Adapter,
                Feature = resolvedFeature.Feature,
                Error = null
            };
        }


        internal static async ValueTask<ResolvedAdapter<TFeature>> ResolveAdapterAsync<TFeature>(
            HttpContext context,
            IAdapterAccessor accessor,
            string adapterId,
            Uri featureId,
            CancellationToken cancellationToken
        ) where TFeature : IAdapterFeature {
            var callContext = new HttpAdapterCallContext(context);

            var resolvedFeature = await accessor.GetAdapterAndFeature<TFeature>(callContext, adapterId, featureId, cancellationToken).ConfigureAwait(false);
            if (!resolvedFeature.IsAdapterResolved) {
                return CreateErrorResult<TFeature>(Results.Problem(statusCode: StatusCodes.Status404NotFound, detail: string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId)));
            }
            if (!resolvedFeature.Adapter.IsEnabled || !resolvedFeature.Adapter.IsRunning) {
                return CreateErrorResult<TFeature>(Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId)));
            }
            if (!resolvedFeature.IsFeatureResolved) {
                return CreateErrorResult<TFeature>(Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: string.Format(callContext.CultureInfo, Resources.Error_UnsupportedInterface, typeof(TFeature).Name)));
            }
            if (!resolvedFeature.IsFeatureAuthorized) {
                return CreateErrorResult<TFeature>(Results.Problem(statusCode: StatusCodes.Status403Forbidden));
            }

            return new ResolvedAdapter<TFeature>() {
                CallContext = callContext,
                Adapter = resolvedFeature.Adapter,
                Feature = resolvedFeature.Feature,
                Error = null
            };
        }


        internal static async ValueTask<ResolvedAdapter<TFeature>> ResolveAdapterAndValidateRequestAsync<TFeature>(
            HttpContext context,
            IAdapterAccessor accessor,
            string adapterId,
            object request,
            bool recursive = true,
            CancellationToken cancellationToken = default
        ) where TFeature : IAdapterFeature {
            var validationError = await ValidateRequestAsync(request, recursive).ConfigureAwait(false);
            if (validationError != null) {
                return CreateErrorResult<TFeature>(validationError);
            }

            var result = await ResolveAdapterAsync<TFeature>(context, accessor, adapterId, cancellationToken).ConfigureAwait(false);

            if (result.Error == null && result.CallContext != null) {
                // Tell the adapter that it doesn't have to revalidate the request.
                result.CallContext.UseRequestValidation(false);
            }

            return result;
        }


        internal static async ValueTask<ResolvedAdapter> ResolveAdapterAsync(
            HttpContext context,
            IAdapterAccessor accessor,
            string adapterId,
            CancellationToken cancellationToken
        ) {
            var callContext = new HttpAdapterCallContext(context);

            var adapter = await accessor.GetAdapter(callContext, adapterId, cancellationToken).ConfigureAwait(false);
            if (adapter == null) {
                return CreateErrorResult(Results.Problem(statusCode: 400, detail: string.Format(callContext.CultureInfo, Resources.Error_CannotResolveAdapterId, adapterId))); // 400
            }
            if (!adapter.IsEnabled || !adapter.IsRunning) {
                return CreateErrorResult(Results.Problem(statusCode: 400, detail: string.Format(callContext.CultureInfo, Resources.Error_AdapterIsNotRunning, adapterId))); // 400
            }

            return new ResolvedAdapter() {
                CallContext = callContext,
                Adapter = adapter,
                Error = null
            };
        }

    }

}
