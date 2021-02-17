using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AssemblyInitializer {

        private static readonly CancellationTokenSource s_cleanupTokenSource = new CancellationTokenSource();

        private static IDisposable s_webHost;

        private static Task s_webHostTask;

        public static IServiceProvider ApplicationServices { get; private set; }


        [AssemblyInitialize]
        public static void Init(TestContext testContext) {
            if (s_webHost != null) {
                return;
            }

            var webHost = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder<WebHostStartup>(Array.Empty<string>())
                .UseUrls(WebHostConfiguration.DefaultUrl)
                .UseKestrel(options => {
                    options.ConfigureEndpointDefaults(listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
                })
                .Build();
            ApplicationServices = webHost.Services;
            webHost.Start();
            s_webHostTask = webHost.WaitForShutdownAsync(s_cleanupTokenSource.Token);
            s_webHost = webHost;
        }


        [AssemblyCleanup]
        public static async Task Cleanup() {
            s_cleanupTokenSource.Cancel();
            if (s_webHost is Process p) {
                p.Kill();
            }

            if (s_webHostTask != null) {
                await s_webHostTask.ConfigureAwait(false);
            }
            s_webHost?.Dispose();
        }

    }
}
