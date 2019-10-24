using System.Linq;

namespace DataCore.Adapter.Diagnostics {
    internal static class GrpcDiagnosticsExtensions {

        internal static Diagnostics.HealthStatus ToAdapterHealthStatus(this Grpc.HealthStatus status) {
            switch (status) {
                case Grpc.HealthStatus.Healthy:
                    return Diagnostics.HealthStatus.Healthy;
                case Grpc.HealthStatus.Degraded:
                    return Diagnostics.HealthStatus.Degraded;
                case Grpc.HealthStatus.Unhealthy:
                default:
                    return Diagnostics.HealthStatus.Unhealthy;
            }
        }


        internal static Grpc.HealthStatus ToGrpcHealthStatus(this Diagnostics.HealthStatus status) {
            switch (status) {
                case Diagnostics.HealthStatus.Healthy:
                    return Grpc.HealthStatus.Healthy;
                case Diagnostics.HealthStatus.Degraded:
                    return Grpc.HealthStatus.Degraded;
                case Diagnostics.HealthStatus.Unhealthy:
                default:
                    return Grpc.HealthStatus.Unhealthy;
            }
        }


        internal static Diagnostics.HealthCheckResult ToAdapterHealthCheckResult(this Grpc.HealthCheckResult healthCheckResult) {
            if (healthCheckResult == null) {
                return Diagnostics.HealthCheckResult.Unhealthy();
            }

            return new Diagnostics.HealthCheckResult(
                healthCheckResult.Status.ToAdapterHealthStatus(),
                healthCheckResult.Description,
                healthCheckResult.Error,
                healthCheckResult.Data,
                healthCheckResult.InnerResults?.Select(x => x.ToAdapterHealthCheckResult()).ToArray()
            );
        }


        internal static Grpc.HealthCheckResult ToGrpcHealthCheckResult(this Diagnostics.HealthCheckResult healthCheckResult) {
            var result = new Grpc.HealthCheckResult() {
                Status = healthCheckResult.Status.ToGrpcHealthStatus(),
                Description = healthCheckResult.Description ?? string.Empty,
                Error = healthCheckResult.Error ?? string.Empty
            };

            if (healthCheckResult.Data != null) {
                result.Data.Add(healthCheckResult.Data);
            }

            if (healthCheckResult.InnerResults != null) {
                foreach (var item in healthCheckResult.InnerResults) {
                    result.InnerResults.Add(item.ToGrpcHealthCheckResult());
                }
            }

            return result;
        }

    }
}
