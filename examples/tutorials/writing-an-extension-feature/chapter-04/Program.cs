using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyAdapter {
    class Program {

        public static async Task Main(params string[] args) {
            await CreateHostBuilder(args).RunConsoleAsync().ConfigureAwait(false);
        }


        private static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args).ConfigureLogging(options => {
                options.SetMinimumLevel(LogLevel.Error);
            }).ConfigureServices(services => {
                services.AddHostedService<Runner>();
            });
        }

    }

}
