using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyAdapter {
    class Program {

        public static async Task Main(params string[] args) {
            await CreateHostBuilder(args).RunConsoleAsync().ConfigureAwait(false);
        }


        private static IHostBuilder CreateHostBuilder(string[] args) {
            return Host.CreateDefaultBuilder(args).ConfigureServices(services => {
                services.AddHostedService<Runner>();
            });
        }

    }

}
