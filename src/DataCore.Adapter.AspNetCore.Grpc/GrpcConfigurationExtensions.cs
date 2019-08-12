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
            return endpoints.MapDataCoreGrpcServices(null);
        }


        /// <summary>
        /// Registers adapter gRPC services.
        /// </summary>
        /// <param name="endpoints">
        ///   The endpoint route builder.
        /// </param>
        /// <param name="builder">
        ///   A callback function that will be invoked for each gRPC service that is registered 
        ///   with the host. The parameters are the type of the gRPC service and the 
        ///   <see cref="IEndpointConventionBuilder"/> for the service endpoint registration. This 
        ///   can be used to e.g. require specific authentication schemes for calling gRPC services 
        ///   such as client certificate authentication.
        /// </param>
        /// <returns>
        ///   The endpoint route builder.
        /// </returns>
        public static IEndpointRouteBuilder MapDataCoreGrpcServices(this IEndpointRouteBuilder endpoints, Action<Type, IEndpointConventionBuilder> builder) { 
            builder?.Invoke(typeof(AdaptersServiceImpl), endpoints.MapGrpcService<AdaptersServiceImpl>());
            builder?.Invoke(typeof(AssetModelBrowserServiceImpl), endpoints.MapGrpcService<AssetModelBrowserServiceImpl>());
            builder?.Invoke(typeof(EventsServiceImpl), endpoints.MapGrpcService<EventsServiceImpl>());
            builder?.Invoke(typeof(HostInfoServiceImpl), endpoints.MapGrpcService<HostInfoServiceImpl>());
            builder?.Invoke(typeof(TagSearchServiceImpl), endpoints.MapGrpcService<TagSearchServiceImpl>());
            builder?.Invoke(typeof(TagValueAnnotationsServiceImpl), endpoints.MapGrpcService<TagValueAnnotationsServiceImpl>());
            builder?.Invoke(typeof(TagValuesServiceImpl), endpoints.MapGrpcService<TagValuesServiceImpl>());

            return endpoints;
        }

    }
}
