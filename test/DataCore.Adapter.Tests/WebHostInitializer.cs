using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class WebHostInitializer {

        private static readonly CancellationTokenSource s_cleanupTokenSource = new CancellationTokenSource();

        private static Task s_webHostTask;

        public static IServiceProvider ApplicationServices { get; internal set; }

        
        [AssemblyInitialize]
        public static void Init(TestContext testContext) {
            if (s_webHostTask != null) {
                return;
            }

            s_webHostTask = WebHost.CreateDefaultBuilder<WebHostStartup>(Array.Empty<string>())
                .UseUrls(WebHostStartup.DefaultUrl)
                .UseKestrel(options => {
                    options.ConfigureEndpointDefaults(listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
                })
                .Build()
                .StartAsync(s_cleanupTokenSource.Token);
        }


        [AssemblyCleanup]
        public static async Task Cleanup() {
            s_cleanupTokenSource.Cancel();
            if (s_webHostTask != null) {
                await s_webHostTask;
            }
        }

    }
}
