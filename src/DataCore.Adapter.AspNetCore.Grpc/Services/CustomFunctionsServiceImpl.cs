using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Extensions;
using DataCore.Adapter.Extensions;

using Grpc.Core;

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.Grpc.Server.Services {

    /// <summary>
    /// Implements <see cref="CustomFunctionsService.CustomFunctionsServiceBase"/>
    /// </summary>
    public class CustomFunctionsServiceImpl : CustomFunctionsService.CustomFunctionsServiceBase {

        /// <summary>
        /// The service for resolving adapter references.
        /// </summary>
        private readonly IAdapterAccessor _adapterAccessor;

        /// <summary>
        /// The JSON serialization options to use.
        /// </summary>
        private readonly JsonSerializerOptions? _jsonOptions;


        /// <summary>
        /// Creates a new <see cref="CustomFunctionsServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        /// <param name="jsonOptions">
        ///   The configured JSON options.
        /// </param>
        public CustomFunctionsServiceImpl(IAdapterAccessor adapterAccessor, IOptions<JsonOptions> jsonOptions) {
            _adapterAccessor = adapterAccessor;
            _jsonOptions = jsonOptions?.Value?.SerializerOptions;
        }


        /// <inheritdoc/>
        public override async Task<GetCustomFunctionsResponse> GetCustomFunctions(GetCustomFunctionsRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ICustomFunctions>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            var functions = await adapter.Feature.GetFunctionsAsync(adapterCallContext, new Extensions.GetCustomFunctionsRequest() {
                PageSize = request.PageSize,
                Page = request.Page,
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                Properties = request.Properties.ToDictionary(x => x.Key, x => x.Value)
            }, cancellationToken).ConfigureAwait(false);
            var result = new GetCustomFunctionsResponse();
            result.Functions.AddRange(functions.Select(x => x.ToGrpcCustomFunctionDescriptor()));
            return result;
        }


        /// <inheritdoc/>
        public override async Task<GetCustomFunctionResponse> GetCustomFunction(GetCustomFunctionRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ICustomFunctions>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            if (!Uri.TryCreate(request.FunctionId, UriKind.RelativeOrAbsolute, out var functionId)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, SharedResources.Error_InvalidUri));
            }

            var function = await adapter.Feature.GetFunctionAsync(adapterCallContext, new Extensions.GetCustomFunctionRequest() {
                Id = functionId,
                Properties = request.Properties.ToDictionary(x => x.Key, x => x.Value)
            }, cancellationToken).ConfigureAwait(false);
            var response = new GetCustomFunctionResponse();
            if (function != null) {
                response.Function = function.ToGrpcCustomFunctionDescriptorExtended();
            }
            return response;
        }


        /// <inheritdoc/>
        public override async Task<InvokeCustomFunctionsResponse> InvokeCustomFunction(InvokeCustomFunctionRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ICustomFunctions>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            if (!Uri.TryCreate(request.FunctionId, UriKind.RelativeOrAbsolute, out var functionId)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, SharedResources.Error_InvalidUri));
            }

            var req = new CustomFunctionInvocationRequest() {
                Id = functionId,
                Body = request.Body.ToJsonElement(),
                Properties = request.Properties.ToDictionary(x => x.Key, x => x.Value)
            };

            Util.ValidateObject(req);

            var function = await adapter.Feature.GetFunctionAsync(adapterCallContext, new Extensions.GetCustomFunctionRequest() {
                Id = req.Id,
            }, cancellationToken).ConfigureAwait(false);

            if (function == null) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, string.Format(CultureInfo.CurrentCulture, AbstractionsResources.Error_UnableToResolveCustomFunction, req.Id)));
            }

            if (!req.TryValidateBody(function, _jsonOptions, out var validationResults)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, JsonSerializer.Serialize(validationResults, _jsonOptions)));
            }

            var result = await adapter.Feature.InvokeFunctionAsync(adapterCallContext, req, cancellationToken).ConfigureAwait(false);
            var response = new InvokeCustomFunctionsResponse() {
                Body = result.Body?.ToProtoValue() ?? Google.Protobuf.WellKnownTypes.Value.ForNull()
            };
            return response;
        }

    }
}
