using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using IntelligentPlant.BackgroundTasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if NET48
using GrpcCore = Grpc.Core;
#else
using Microsoft.AspNetCore.Hosting;
#endif

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AssemblyInitializer {

#if NET48
        private const bool RunExternalWebHost = true;
#else
        private const bool RunExternalWebHost = false;
#endif

        private static readonly CancellationTokenSource s_cleanupTokenSource = new CancellationTokenSource();

        private static IDisposable s_webHost;

        private static Task s_webHostTask;

        public static IServiceProvider ApplicationServices { get; private set; }

        
        [AssemblyInitialize]
        public static async Task Init(TestContext testContext) {
            if (s_webHost != null) {
                return;
            }

            if (RunExternalWebHost) {
                var services = new ServiceCollection();
                WebHostConfiguration.ConfigureDefaultServices(services);
                services.AddAspNetCoreBackgroundTaskService();

#if NET48
                // Can't use Grpc.Net with .NET Framework, so need to allow gRPC proxies to be created using a Grpc.Core channel.
                services.AddTransient(sp => {
                    var certificatePath = "cert:/CurrentUser/My/8a5678cfa914795fa2d8ab854abfe73448e26157";
                    var sslCredentials = Security.CertificateUtilities.TryLoadCertificateFromStore(certificatePath, out var cert)
                        ? new GrpcCore.SslCredentials(Security.CertificateUtilities.PemEncode(cert))
                        : new GrpcCore.SslCredentials();
                    return new GrpcCore.Channel(WebHostConfiguration.DefaultHostName, WebHostConfiguration.DefaultPortNumber, sslCredentials);
                });
#endif

                ApplicationServices = services.BuildServiceProvider();
                var backgroundTaskService = (BackgroundTaskService) ApplicationServices.GetService<IBackgroundTaskService>();
                _ = backgroundTaskService.RunAsync(s_cleanupTokenSource.Token);

                const string webHostAsm = "DataCore.Adapter.Tests.WebHost.exe";
                var webHostBaseDir = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppContext.BaseDirectory, "webhost")).FullName;
                var webHostFullPath = System.IO.Path.Combine(webHostBaseDir, webHostAsm);

                var process = new Process() {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo() { 
                        FileName = webHostFullPath,
                        WorkingDirectory = webHostBaseDir,
                        Arguments = $"{Process.GetCurrentProcess().Id}",
                        UseShellExecute = true
                    }
                };

                var tcs = new TaskCompletionSource<int>();
                process.Exited += (sender, args) => {
                    tcs.TrySetResult(process.ExitCode);
                };

                s_webHostTask = tcs.Task;
                process.Start();
                s_webHost = process;

                await Task.Delay(5000, s_cleanupTokenSource.Token).ConfigureAwait(false);
            }
#if NET48 == false
            else {
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
#endif
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
