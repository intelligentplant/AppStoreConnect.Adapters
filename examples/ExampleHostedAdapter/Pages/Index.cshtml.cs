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

        public ApiInfo RestApi { get; }

        public ApiInfo SignalRApi { get; }

        public ApiInfo GrpcApi { get; }

        public bool ApiIsEnabled => RestApi.IsEnabled || SignalRApi.IsEnabled || GrpcApi.IsEnabled;


        public IndexModel(HostInfo hostInfo, IAdapter adapter, EndpointDataSource endpointDataSource, ILogger<IndexModel> logger) {
            HostInfo = hostInfo;
            Adapter = adapter;
            _logger = logger;

            if (endpointDataSource.IsMvcAdapterApiRegistered()) {
                RestApi = new ApiInfo() { IsEnabled = true, Version = typeof(AdapterMvcEndpointDataSourceExtensions).Assembly.GetName()?.Version?.ToString(3) };
            }

            if (endpointDataSource.IsSignalRAdapterApiRegistered()) {
                SignalRApi = new ApiInfo() { IsEnabled = true, Version = typeof(AdapterSignalREndpointDataSourceExtensions).Assembly.GetName()?.Version?.ToString(3) };
            }

            if (endpointDataSource.IsGrpcAdapterApiRegistered()) {
                GrpcApi = new ApiInfo() { IsEnabled = true, Version = typeof(AdapterGrpcEndpointDataSourceExtensions).Assembly.GetName()?.Version?.ToString(3) };
            }
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


        public readonly record struct ApiInfo {

            public readonly bool IsEnabled { get; init; }

            public readonly string? Version { get; init; }

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
