using DataCore.Adapter.Common;

using Microsoft.AspNetCore.Routing;

namespace DataCore.Adapter.AspNetCore.Internal {

    /// <summary>
    /// <see cref="IApiDescriptorProvider"/> for the adapter REST API.
    /// </summary>
    internal sealed class ApiDescriptorProvider : IApiDescriptorProvider {

        /// <summary>
        /// The ASP.NET Core <see cref="EndpointDataSource"/>.
        /// </summary>
        private readonly EndpointDataSource _endpointDataSource;


        /// <summary>
        /// Creates a new <see cref="ApiDescriptorProvider"/> instance.
        /// </summary>
        /// <param name="endpointDataSource">
        ///   The ASP.NET Core <see cref="EndpointDataSource"/>.
        /// </param>
        public ApiDescriptorProvider(EndpointDataSource endpointDataSource) {
            _endpointDataSource = endpointDataSource;
        }


        /// <inheritdoc/>
        public ApiDescriptor GetApiDescriptor() {
            var asmName = GetType().Assembly.GetName();
            return new ApiDescriptor("REST", asmName.Name, asmName.Version!.ToString(3), _endpointDataSource.IsAdapterMinimalApiRegistered());
        }

    }

}
