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
        public static IEndpointRouteBuilder MapDataCoreGrpcServices(this IEndpointRouteBuilder endpoints, Action<Type, IEndpointConventionBuilder>? builder) {
            MapService<AdaptersServiceImpl>(endpoints, builder);
            MapService<AssetModelBrowserServiceImpl>(endpoints, builder);
            MapService<ConfigurationChangesServiceImpl>(endpoints, builder);
            MapService<EventsServiceImpl>(endpoints, builder);
            MapService<HostInfoServiceImpl>(endpoints, builder);
            MapService<TagSearchServiceImpl>(endpoints, builder);
            MapService<TagValueAnnotationsServiceImpl>(endpoints, builder);
            MapService<TagValuesServiceImpl>(endpoints, builder);
            MapService<ExtensionFeaturesServiceImpl>(endpoints, builder);

            return endpoints;
        }


        /// <summary>
        /// Registers a gRPC service.
        /// </summary>
        /// <typeparam name="TService">
        ///   The service type.
        /// </typeparam>
        /// <param name="endpoints">
        ///   The endpoint route builder to register the service with.
        /// </param>
        /// <param name="builder">
        ///   The optional builder for the registered endpoint.
        /// </param>
        private static void MapService<TService>(IEndpointRouteBuilder endpoints, Action<Type, IEndpointConventionBuilder>? builder) where TService : class {
            var endpoint = endpoints.MapGrpcService<TService>();
            builder?.Invoke(typeof(TService), endpoint);
        }

    }
}
