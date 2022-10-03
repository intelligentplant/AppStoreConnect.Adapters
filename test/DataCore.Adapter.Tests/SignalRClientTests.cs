#if NETCOREAPP

using System;
using System.Linq;
using System.Threading.Tasks;

using DataCore.Adapter.AspNetCore.SignalR.Client;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class SignalRClientTests : TestsBase {

        [TestMethod]
        public async Task GetSupportedDataFunctionsShouldReturnResultsWithoutRequestObject() {
            var builder = new HubConnectionBuilder()
                .WithUrl(WebHostConfiguration.DefaultUrl + SignalRConfigurationExtensions.HubRoute, options => {
                    options.HttpMessageHandlerFactory = handler => {
                        WebHostConfiguration.AllowUntrustedCertificates(handler);
                        return handler;
                    };
                })
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                })
                .WithAutomaticReconnect();

            await using (var client = new AdapterSignalRClient(builder.Build())) {
                CancelAfter(TimeSpan.FromSeconds(10));

                var funcs = await client.TagValues.GetSupportedDataFunctionsAsync(
                    WebHostConfiguration.AdapterId, 
                    CancellationToken
                ).ToEnumerable(CancellationToken).ConfigureAwait(false);

                Assert.IsNotNull(funcs);
                Assert.IsTrue(funcs.Any());
            }
        }


        [TestMethod]
        public async Task GetSupportedDataFunctionsShouldReturnResultsWithRequestObject() {
            var builder = new HubConnectionBuilder()
                .WithUrl(WebHostConfiguration.DefaultUrl + SignalRConfigurationExtensions.HubRoute, options => {
                    options.HttpMessageHandlerFactory = handler => {
                        WebHostConfiguration.AllowUntrustedCertificates(handler);
                        return handler;
                    };
                })
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                })
                .WithAutomaticReconnect();

            await using (var client = new AdapterSignalRClient(builder.Build())) {
                CancelAfter(TimeSpan.FromSeconds(10));

                var funcs = await client.TagValues.GetSupportedDataFunctionsAsync(
                    WebHostConfiguration.AdapterId, 
                    new RealTimeData.GetSupportedDataFunctionsRequest(),
                    CancellationToken
                ).ToEnumerable(CancellationToken).ConfigureAwait(false);

                Assert.IsNotNull(funcs);
                Assert.IsTrue(funcs.Any());
            }
        }

    }

}

#endif
