using System.Reflection;

using DataCore.Adapter;

namespace OpenTelemetry.Resources {

    /// <summary>
    /// Extensions for <see cref="ResourceBuilder"/>.
    /// </summary>
    public static class DCAResourceBuilderExtensions {

        /// <summary>
        /// The OpenTelemetry service name for an adapter API host.
        /// </summary>
        public const string AdapterApiServiceName = "Adapter API";

        /// <summary>
        /// Adds an Adapter API service with optional instance ID to the <see cref="ResourceBuilder"/>.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="ResourceBuilder"/>.
        /// </param>
        /// <param name="serviceInstanceId">
        ///   The instance ID for the service. Specify <see langword="null"/> or white space to use 
        ///   the DNS host name of the local machine.
        /// </param>
        /// <returns>
        ///   The <see cref="ResourceBuilder"/>.
        /// </returns>
        public static ResourceBuilder AddDataCoreAdapterApiService(this ResourceBuilder builder, string? serviceInstanceId = null) {
            return builder.AddService(
                serviceName: AdapterApiServiceName, 
                serviceVersion: Assembly.GetEntryAssembly().GetInformationalVersion(), 
                serviceInstanceId: string.IsNullOrWhiteSpace(serviceInstanceId) 
                    ? System.Net.Dns.GetHostName() 
                    : serviceInstanceId
            );
        }

    }
}
