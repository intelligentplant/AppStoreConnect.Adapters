using System;

using DataCore.Adapter.AspNetCore.SignalR.Proxy;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    public abstract class SignalRProxyTests : ProxyAdapterTests<SignalRAdapterProxy> {

        protected sealed override SignalRAdapterProxy CreateProxy(string remoteAdapterId, IServiceProvider serviceProvider) {
            return ActivatorUtilities.CreateInstance<SignalRAdapterProxy>(serviceProvider, nameof(SignalRProxyTests), new SignalRAdapterProxyOptions() {
                RemoteId = remoteAdapterId,
                ConnectionFactory = key => {
                    var builder = new HubConnectionBuilder()
                        .WithUrl(WebHostConfiguration.DefaultUrl + SignalRConfigurationExtensions.HubRoute, options => {
                            options.HttpMessageHandlerFactory = handler => {
                                WebHostConfiguration.AllowUntrustedCertificates(handler);
                                return handler;
                            };
                        })
                        .WithAutomaticReconnect();
                    return AddProtocol(builder).Build();
                }
            });
        }


        protected abstract IHubConnectionBuilder AddProtocol(IHubConnectionBuilder builder);

    }


    [TestClass]
    public class SignalRProxyJsonTests : SignalRProxyTests {

        protected override IHubConnectionBuilder AddProtocol(IHubConnectionBuilder builder) {
            return builder.AddJsonProtocol(options => {
                Json.JsonSerializerOptionsExtensions.AddDataCoreAdapterConverters(options.PayloadSerializerOptions.Converters);
                options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });
        }

    }


    [TestClass]
    public class SignalRProxyMessagePackTests : SignalRProxyTests {

        protected override IHubConnectionBuilder AddProtocol(IHubConnectionBuilder builder) {
            return builder.AddMessagePackProtocol();
        }

    }
}
