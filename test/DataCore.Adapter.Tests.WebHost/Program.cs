
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace DataCore.Adapter.Tests {
    public class Program {
        public static async Task Main(string[] args) {
            using (var ctSource = new CancellationTokenSource())
            using (var host = CreateHostBuilder(args).Build()) {
                // args[0] should be the PID of the process that started the web host. If it is 
                // specified, we will monitor that process and exit when that process exits. 
                // Otherwise, we'll automatically exit after 5 minutes.
                Process process = null;
                if (args.Length > 0 && int.TryParse(args[0], out var pid)) {
                    process = Process.GetProcessById(pid);
                }

                if (process == null) {
                    var prev = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"[[ NO PID SPECIFIED: AUTOMATIC SHUTDOWN AT {DateTime.Now.AddMinutes(5):HH:mm:ss} ]]");
                    Console.ForegroundColor = prev;
                    ctSource.CancelAfter(TimeSpan.FromMinutes(5));
                }
                else {
                    var prev = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"[[ MONITORING PID {process.Id} ({process.ProcessName}) FOR EXIT ]]");
                    Console.ForegroundColor = prev;
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, args) => {
                        ctSource.Cancel();
                    };
                }

                host.Start();
                await host.WaitForShutdownAsync(ctSource.Token);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder
                        .UseStartup<WebHostStartup>().UseUrls(WebHostConfiguration.DefaultUrl)
                        .UseKestrel(options => {
                            options.ConfigureEndpointDefaults(listen => listen.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
                        });
                });
    }
}
