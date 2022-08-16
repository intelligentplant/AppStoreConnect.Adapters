using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AdapterOptionsTests : TestsBase {

        private IServiceProvider ConfigureServiceProvider(string optionsKey, IConfiguration configuration) {
            var services = new ServiceCollection();

            if (string.IsNullOrWhiteSpace(optionsKey)) {
                services.Configure<OptionsMonitorAdapterOptions>(configuration);
            }
            else {
                services.Configure<OptionsMonitorAdapterOptions>(optionsKey, configuration);
            }

            return services.BuildServiceProvider();
        }


        [TestMethod]
        public void AdapterNameAndDescriptionShouldBeSetFromOptions() {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var provider = configuration.Providers.OfType<MemoryConfigurationProvider>().First();

            var id = Guid.NewGuid().ToString();
            var name = TestContext.TestName;
            var description = TestContext.FullyQualifiedTestClassName;

            provider.Set(nameof(AdapterOptions.Name), name);
            provider.Set(nameof(AdapterOptions.Description), description);

            var serviceProvider = ConfigureServiceProvider(null, configuration);

            using (var adapter = new OptionsMonitorAdapter(
                id, 
                serviceProvider.GetRequiredService<IOptions<OptionsMonitorAdapterOptions>>())
            ) {
                Assert.AreEqual(name, adapter.Descriptor.Name);
                Assert.AreEqual(description, adapter.Descriptor.Description);
            }
        }



        [TestMethod]
        public void AdapterNameAndDescriptionShouldBeSetFromNamedOptionsMonitor() {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var provider = configuration.Providers.OfType<MemoryConfigurationProvider>().First();

            var id = Guid.NewGuid().ToString();
            var name = TestContext.TestName;
            var description = TestContext.FullyQualifiedTestClassName;

            provider.Set(nameof(AdapterOptions.Name), name);
            provider.Set(nameof(AdapterOptions.Description), description);

            var serviceProvider = ConfigureServiceProvider(id, configuration);

            using (var adapter = new OptionsMonitorAdapter(
                id,
                serviceProvider.GetRequiredService<IOptionsMonitor<OptionsMonitorAdapterOptions>>())
            ) {
                Assert.AreEqual(name, adapter.Descriptor.Name);
                Assert.AreEqual(description, adapter.Descriptor.Description);
            }
        }


        [TestMethod]
        public void AdapterNameShouldBeUpdatedUsingNamedOptionsMonitor() {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var provider = configuration.Providers.OfType<MemoryConfigurationProvider>().First();

            var id = Guid.NewGuid().ToString();
            var name = TestContext.TestName;
            var description = TestContext.FullyQualifiedTestClassName;

            provider.Set(nameof(AdapterOptions.Name), name);
            provider.Set(nameof(AdapterOptions.Description), description);

            var serviceProvider = ConfigureServiceProvider(id, configuration);

            using (var adapter = new OptionsMonitorAdapter(
                id,
                serviceProvider.GetRequiredService<IOptionsMonitor<OptionsMonitorAdapterOptions>>())
            ) {
                Assert.AreEqual(name, adapter.Descriptor.Name);
                Assert.AreEqual(description, adapter.Descriptor.Description);

                name = string.Join("|", name, "UPDATED");
                provider.Set(nameof(AdapterOptions.Name), name);
                configuration.Reload();

                Assert.AreEqual(name, adapter.Descriptor.Name);
            }
        }


        [TestMethod]
        public void AdapterDescriptionShouldBeUpdatedUsingNamedOptionsMonitor() {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var provider = configuration.Providers.OfType<MemoryConfigurationProvider>().First();

            var id = Guid.NewGuid().ToString();
            var name = TestContext.TestName;
            var description = TestContext.FullyQualifiedTestClassName;

            provider.Set(nameof(AdapterOptions.Name), name);
            provider.Set(nameof(AdapterOptions.Description), description);

            var serviceProvider = ConfigureServiceProvider(id, configuration);

            using (var adapter = new OptionsMonitorAdapter(
                id,
                serviceProvider.GetRequiredService<IOptionsMonitor<OptionsMonitorAdapterOptions>>())
            ) {
                Assert.AreEqual(name, adapter.Descriptor.Name);
                Assert.AreEqual(description, adapter.Descriptor.Description);

                description = string.Join("|", description, "UPDATED");
                provider.Set(nameof(AdapterOptions.Description), description);
                configuration.Reload();

                Assert.AreEqual(description, adapter.Descriptor.Description);
            }
        }


        [TestMethod]
        public void UtcOptionsTimestampShouldChangeAfterConfigurationReload() {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var provider = configuration.Providers.OfType<MemoryConfigurationProvider>().First();

            var id = Guid.NewGuid().ToString();
            var name = TestContext.TestName;
            var description = TestContext.FullyQualifiedTestClassName;

            provider.Set(nameof(AdapterOptions.Name), name);
            provider.Set(nameof(AdapterOptions.Description), description);

            var serviceProvider = ConfigureServiceProvider(id, configuration);

            using (var adapter = new OptionsMonitorAdapter(
                id,
                serviceProvider.GetRequiredService<IOptionsMonitor<OptionsMonitorAdapterOptions>>())
            ) {
                Assert.AreEqual(name, adapter.Descriptor.Name);
                Assert.AreEqual(description, adapter.Descriptor.Description);

                var time = adapter.UtcOptionsTime;
                provider.Set(nameof(OptionsMonitorAdapterOptions.UtcOptionsTime), time.AddHours(1).ToString("u"));
                configuration.Reload();

                Assert.IsTrue(adapter.UtcOptionsTime > time);
            }
        }


        [TestMethod]
        public async Task AdapterShouldStopWhenEnabledStatusChanges() {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var provider = configuration.Providers.OfType<MemoryConfigurationProvider>().First();

            var id = Guid.NewGuid().ToString();
            var name = TestContext.TestName;
            var description = TestContext.FullyQualifiedTestClassName;

            provider.Set(nameof(AdapterOptions.Name), name);
            provider.Set(nameof(AdapterOptions.Description), description);

            var serviceProvider = ConfigureServiceProvider(id, configuration);
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var adapter = new OptionsMonitorAdapter(
                id,
                serviceProvider.GetRequiredService<IOptionsMonitor<OptionsMonitorAdapterOptions>>())
            ) {
                await ((IAdapter) adapter).StartAsync(default).ConfigureAwait(false);
                Assert.IsTrue(adapter.IsEnabled);
                Assert.IsTrue(adapter.IsRunning);

                using (adapter.StopToken.Register(() => tcs.TrySetResult(null))) {
                    provider.Set(nameof(AdapterOptions.IsEnabled), false.ToString());
                    configuration.Reload();
                    await tcs.Task.WithCancellation(CancellationToken).ConfigureAwait(false);
                }

                Assert.IsFalse(adapter.IsEnabled);
                Assert.IsFalse(adapter.IsRunning);
            }
        }

    }
}
