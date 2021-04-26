#if NETCOREAPP
extern alias DataCoreAdapterGrpcClient;

using System;
using System.Threading.Tasks;

using DataCore.Adapter.Grpc.Proxy;

using DataCoreAdapterGrpcClient::DataCore.Adapter.Grpc;

using Grpc.Core;
using Grpc.Core.Interceptors;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class GrpcProxyTests : ProxyAdapterTests<GrpcAdapterProxy> {

        protected override GrpcAdapterProxy CreateProxy(TestContext context, string remoteAdapterId, IServiceProvider serviceProvider) {
            var options = new GrpcAdapterProxyOptions() {
                RemoteId = remoteAdapterId
            };

            if (string.Equals(context.TestName, nameof(GrpcAdapterProxyShouldAddInterceptorToClient))) {
                var interceptor = new TestInterceptor();
                context.Properties.Add(nameof(TestInterceptor), interceptor);
                options.GetClientInterceptors = () => {
                    return new[] {
                        interceptor
                    };
                };
            }

            return ActivatorUtilities.CreateInstance<GrpcAdapterProxy>(serviceProvider, nameof(GrpcProxyTests), options);
        }


        [TestMethod]
        public Task GrpcAdapterProxyShouldAddInterceptorToClient() {
            return RunAdapterTest(async (adapter, ctx, ct) => {
                var interceptor = TestContext.Properties[nameof(TestInterceptor)] as TestInterceptor;
                Assert.IsNotNull(interceptor);

                var client = adapter.CreateClient<HostInfoService.HostInfoServiceClient>();

                Assert.IsFalse(interceptor.Intercepted);
                var hostInfo = client.GetHostInfoAsync(new GetHostInfoRequest(), cancellationToken: ct);
                var response = await hostInfo.ResponseAsync.ConfigureAwait(false);
                Assert.IsTrue(interceptor.Intercepted);

            }, false);
        }


        private class TestInterceptor : Interceptor {

            public bool Intercepted { get; private set; }

            public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
                Intercepted = true;
                return continuation(request, context);
            }

        }

    }
}
#endif
