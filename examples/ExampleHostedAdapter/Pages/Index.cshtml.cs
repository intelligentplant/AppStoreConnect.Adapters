using DataCore.Adapter;
using DataCore.Adapter.AspNetCore;
using DataCore.Adapter.Common;
using DataCore.Adapter.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExampleHostedAdapter.Pages {
    public class IndexModel : PageModel {
        private readonly ILogger<IndexModel> _logger;

        public HostInfo HostInfo { get; }

        public IAdapter Adapter { get; }

        public AdapterState State { get; private set; } 

        public ApiDescriptor[] AvailableApis { get; }

        public bool ApiIsEnabled => AvailableApis.Any(x => x.Enabled);


        public IndexModel(HostInfo hostInfo, IAdapter adapter, IAvailableApiService availableApiService, ILogger<IndexModel> logger) {
            HostInfo = hostInfo;
            Adapter = adapter;
            _logger = logger;

            AvailableApis = availableApiService.GetApiDescriptors().OrderBy(x => x.Name).ToArray();
        }


        public async Task OnGetAsync(CancellationToken cancellationToken) {
            if (!Adapter.IsEnabled) {
                State = AdapterState.Disabled;
                return;
            }
            if (!Adapter.IsRunning) {
                State = AdapterState.Enabled;
                return;
            }

            var healthCheck = Adapter.GetFeature<IHealthCheck>();
            if (healthCheck == null) {
                State = AdapterState.Running;
                return;
            }

            var context = new HttpAdapterCallContext(HttpContext);
            var healthCheckResult = await healthCheck.CheckHealthAsync(context, cancellationToken);

            State = healthCheckResult.Status == HealthStatus.Healthy
                ? AdapterState.Running
                : AdapterState.RunningWithWarning;
        }


        public async Task<IActionResult> OnGetStatusAsync(CancellationToken cancellationToken) {
            await OnGetAsync(cancellationToken);
            return Partial("_AdapterStatusPartial", this);
        }


        public string GetHostId() {
            var prop = HostInfo.Properties.FindProperty("InstanceId");
            if (prop == null) {
                return "<unspecified>";
            }

            return prop.Value.GetValueOrDefault("<unspecified>")!;
        }


        public enum AdapterState {
            Unknown,
            Disabled,
            Enabled,
            Running,
            RunningWithWarning
        }

    }
}
