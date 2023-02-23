using DataCore.Adapter.Common;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Routing.V2 {
    internal class HostInfoRoutes : IRouteProvider {
        public static void Register(IEndpointRouteBuilder builder) {
            builder.MapGet("/", GetHostInfo);
            builder.MapGet("/adapter-features", GetStandardFeatureDescriptors);
            builder.MapGet("/available-apis", GetAvailableApis);
        }


        private static IResult GetHostInfo(HostInfo hostInfo) {
            return Results.Ok(hostInfo);
        }


        private static IResult GetStandardFeatureDescriptors() {
            return Results.Ok(TypeExtensions.GetStandardAdapterFeatureTypes().Select(x => x.CreateFeatureDescriptor()).ToArray());
        }


        private static IResult GetAvailableApis(IAvailableApiService availableApiService) {
            return Results.Ok(availableApiService.GetApiDescriptors().Where(x => x.Enabled).ToArray());
        }

    }
}
