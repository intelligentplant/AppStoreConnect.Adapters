using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Grpc.Proxy.Extensions.Features {

    /// <summary>
    /// <see cref="ICustomFunctions"/> implementation.
    /// </summary>
    internal class CustomFunctionsImpl : ProxyAdapterFeature, ICustomFunctions {

        /// <summary>
        /// Creates a new <see cref="CustomFunctionsImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public CustomFunctionsImpl(GrpcAdapterProxy proxy) : base(proxy) { }


        /// <inheritdoc/>
        public async Task<IEnumerable<Adapter.Extensions.CustomFunctionDescriptor>> GetFunctionsAsync(
            IAdapterCallContext context, 
            Adapter.Extensions.GetCustomFunctionsRequest request,
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<CustomFunctionsService.CustomFunctionsServiceClient>();
            var grpcRequest = new GetCustomFunctionsRequest() { 
                AdapterId = AdapterId,
                PageSize = request.PageSize,
                Page = request.Page,
                Id = request.Id ?? string.Empty,
                Name = request.Name ?? string.Empty,
                Description = request.Description ?? string.Empty,
            };

            if (request.Properties != null) {
                foreach (var item in request.Properties) {
                    grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var grpcResponse = await client.GetCustomFunctionsAsync(grpcRequest, GetCallOptions(context, ctSource.Token)).ConfigureAwait(false);
                if (grpcResponse == null) {
                    return Array.Empty<Adapter.Extensions.CustomFunctionDescriptor>();
                }

                return grpcResponse.Functions.Select(x => x.ToAdapterCustomFunctionDescriptor()).ToArray();
            }
        }


        /// <inheritdoc/>
        public async Task<Adapter.Extensions.CustomFunctionDescriptorExtended?> GetFunctionAsync(
            IAdapterCallContext context, 
            Adapter.Extensions.GetCustomFunctionRequest request, 
            CancellationToken cancellationToken
        ) {
            Proxy.ValidateInvocation(context, request);
            var client = CreateClient<CustomFunctionsService.CustomFunctionsServiceClient>();
            var grpcRequest = new GetCustomFunctionRequest() {
                AdapterId = AdapterId,
                FunctionId = request.Id.ToString()
            };

            if (request.Properties != null) {
                foreach (var item in request.Properties) {
                    grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var grpcResponse = await client.GetCustomFunctionAsync(grpcRequest, GetCallOptions(context, ctSource.Token)).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(grpcResponse?.Function.Function.Id)) {
                    return null;
                }

                return grpcResponse.Function.ToAdapterCustomFunctionDescriptorExtended();
            }
        }


        /// <inheritdoc/>
        public async Task<CustomFunctionInvocationResponse> InvokeFunctionAsync(IAdapterCallContext context, CustomFunctionInvocationRequest request, CancellationToken cancellationToken) {
            Proxy.ValidateInvocation(context, request);

            var client = CreateClient<CustomFunctionsService.CustomFunctionsServiceClient>();
            var grpcRequest = new InvokeCustomFunctionRequest() {
                AdapterId = AdapterId,
                FunctionId = request.Id.ToString(),
                Body = request.Body.ToProtoValue()
            };

            if (request.Properties != null) {
                foreach (var item in request.Properties) {
                    grpcRequest.Properties.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            using (var ctSource = Proxy.CreateCancellationTokenSource(cancellationToken)) {
                var grpcResponse = await client.InvokeCustomFunctionAsync(grpcRequest, GetCallOptions(context, ctSource.Token)).ConfigureAwait(false);
                return new CustomFunctionInvocationResponse() {
                    Body = grpcResponse.Body.ToJsonElement()
                };
            }
        }

    }
}
