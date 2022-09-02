using System;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.Grpc;
using DataCore.Adapter.Diagnostics;
using DataCore.Adapter.Diagnostics.Extensions;
using DataCore.Adapter.Extensions;

using Grpc.Core;

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
        /// Creates a new <see cref="CustomFunctionsServiceImpl"/> object.
        /// </summary>
        /// <param name="adapterAccessor">
        ///   The service for resolving adapter references.
        /// </param>
        public CustomFunctionsServiceImpl(IAdapterAccessor adapterAccessor) {
            _adapterAccessor = adapterAccessor;
        }


        /// <inheritdoc/>
        public override async Task<GetCustomFunctionsResponse> GetCustomFunctions(GetCustomFunctionsRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ICustomFunctions>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            using (Telemetry.ActivitySource.StartGetCustomFunctionsActivity(adapter.Adapter.Descriptor.Id)) {
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
        }


        /// <inheritdoc/>
        public override async Task<GetCustomFunctionResponse> GetCustomFunction(GetCustomFunctionRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ICustomFunctions>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            if (!Uri.TryCreate(request.FunctionId, UriKind.Absolute, out var functionId)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, SharedResources.Error_AbsoluteUriRequired));
            }

            using (Telemetry.ActivitySource.StartGetCustomFunctionActivity(adapter.Adapter.Descriptor.Id, functionId)) {
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
        }


        /// <inheritdoc/>
        public override async Task<InvokeCustomFunctionsResponse> InvokeCustomFunction(InvokeCustomFunctionRequest request, ServerCallContext context) {
            var adapterCallContext = new GrpcAdapterCallContext(context);
            var adapterId = request.AdapterId;
            var cancellationToken = context.CancellationToken;
            var adapter = await Util.ResolveAdapterAndFeature<ICustomFunctions>(adapterCallContext, _adapterAccessor, adapterId, cancellationToken).ConfigureAwait(false);

            if (!Uri.TryCreate(request.FunctionId, UriKind.Absolute, out var functionId)) {
                throw new RpcException(new Status(StatusCode.InvalidArgument, SharedResources.Error_AbsoluteUriRequired));
            }

            var req = new CustomFunctionInvocationRequest() {
                Id = functionId,
                Body = request.Body.ToJsonElement(),
                Properties = request.Properties.ToDictionary(x => x.Key, x => x.Value)
            };

            Util.ValidateObject(req);

            using (Telemetry.ActivitySource.StartInvokeCustomFunctionActivity(adapter.Adapter.Descriptor.Id, req.Id)) {
                var result = await adapter.Feature.InvokeFunctionAsync(adapterCallContext, req, cancellationToken).ConfigureAwait(false);
                var response = new InvokeCustomFunctionsResponse();
                response.Body = Google.Protobuf.JsonParser.Default.Parse<Google.Protobuf.WellKnownTypes.Struct>(result.Body.ToString());
                return response;
            }
        }

    }
}
