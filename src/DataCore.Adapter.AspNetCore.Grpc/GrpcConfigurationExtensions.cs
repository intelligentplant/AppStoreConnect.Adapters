using System;
using DataCore.Adapter.Grpc.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.Extensions.DependencyInjection {

    /// <summary>
    /// Extension methods for registering gRPC adapter services.
    /// </summary>
    public static class GrpcConfigurationExtensions {

        /// <summary>
        /// Registers adapter gRPC services.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <returns>
        ///   The endpoint route builder.
        /// </returns>
        public static IEndpointRouteBuilder MapDataCoreGrpcServices(this IEndpointRouteBuilder endpoints) {
            endpoints.MapGrpcService<AdaptersServiceImpl>();
            endpoints.MapGrpcService<AssetModelBrowserServiceImpl>();
            endpoints.MapGrpcService<EventsServiceImpl>();
            endpoints.MapGrpcService<HostInfoServiceImpl>();
            endpoints.MapGrpcService<TagSearchServiceImpl>();
            endpoints.MapGrpcService<TagValueAnnotationsServiceImpl>();
            endpoints.MapGrpcService<TagValuesServiceImpl>();

            return endpoints;
        }

    }
}
