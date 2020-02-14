using DataCore.Adapter.AspNetCore.SignalR.Proxy;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    public abstract class SignalRProxyTests : ProxyAdapterTests<SignalRAdapterProxy> {

        protected sealed override SignalRAdapterProxy CreateProxy(string remoteAdapterId) {
            return ActivatorUtilities.CreateInstance<SignalRAdapterProxy>(ServiceProvider, new SignalRAdapterProxyOptions() {
                RemoteId = remoteAdapterId,
                ConnectionFactory = key => {
                    var builder = new HubConnectionBuilder()
                        .WithUrl(WebHostStartup.DefaultUrl + SignalRConfigurationExtensions.HubRoute)
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
            return builder.AddJsonProtocol(options => Json.JsonSerializerOptionsExtensions.AddDataCoreAdapterConverters(options.PayloadSerializerOptions.Converters));
        }

    }


    [TestClass]
    public class SignalRProxyMessagePackTests : SignalRProxyTests {

        protected override IHubConnectionBuilder AddProtocol(IHubConnectionBuilder builder) {
            return builder.AddMessagePackProtocol();
        }

    }
}
