#if NETCOREAPP

using System;
using System.Threading.Tasks;
using System.Threading;

using DataCore.Adapter.AspNetCore.SignalR.Proxy;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataCore.Adapter.Json;

namespace DataCore.Adapter.Tests {

    public abstract class SignalRProxyTests : ProxyAdapterTests<SignalRAdapterProxy> {

        protected sealed override SignalRAdapterProxy CreateProxy(TestContext context, string remoteAdapterId, IServiceProvider serviceProvider) {
            return ActivatorUtilities.CreateInstance<SignalRAdapterProxy>(serviceProvider, nameof(SignalRProxyTests), new SignalRAdapterProxyOptions() {
                RemoteId = remoteAdapterId,
                ConnectionFactory = key => {
                    var builder = new HubConnectionBuilder()
                        .WithDataCoreAdapterConnection(WebHostConfiguration.DefaultUrl + SignalRConfigurationExtensions.HubRoute, options => {
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


        protected override async Task BeforeAdapterTestAsync(SignalRAdapterProxy adapter, IAdapterCallContext context, CancellationToken cancellationToken) {
            await base.BeforeAdapterTestAsync(adapter, context, cancellationToken).ConfigureAwait(false);
            // Pre-start the connection to help avoid timeout issues in some tests.
            await adapter.GetClient().GetHubConnection(true, cancellationToken).ConfigureAwait(false);
        }


        protected abstract IHubConnectionBuilder AddProtocol(IHubConnectionBuilder builder);

    }


    [TestClass]
    public class SignalRProxyJsonTests : SignalRProxyTests {

        protected override IHubConnectionBuilder AddProtocol(IHubConnectionBuilder builder) {
            // JSON protocol is already registered; no need to do anything.
            return builder;
        }

    }

}

#endif
