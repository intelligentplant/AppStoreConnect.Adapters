using DataCore.Adapter.Grpc;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents.
    /// </summary>
    public static class GrpcCommonExtensions {

        public static Models.HostInfo ToAdapterHostInfo(this HostInfo hostInfo) {
            if (hostInfo == null) {
                return null;
            }

            return new Models.HostInfo(
                hostInfo.Name,
                hostInfo.Description,
                hostInfo.Version,
                hostInfo.VendorInfo == null 
                    ? null 
                    : new Models.VendorInfo(hostInfo.VendorInfo.Name, hostInfo.VendorInfo.Url),
                hostInfo.Properties
            );
        }


        public static Models.AdapterDescriptor ToAdapterDescriptor(this AdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return new Models.AdapterDescriptor(descriptor.Id, descriptor.Name, descriptor.Description);
        }


        public static Models.AdapterDescriptorExtended ToExtendedAdapterDescriptor(this ExtendedAdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return new Models.AdapterDescriptorExtended(
                descriptor.AdapterDescriptor?.Id,
                descriptor.AdapterDescriptor?.Name,
                descriptor.AdapterDescriptor?.Description,
                descriptor.Features,
                descriptor.Extensions,
                descriptor.Properties
            );
        }


        public static Models.WriteStatus ToAdapterWriteStatus(this WriteOperationStatus status) {
            switch (status) {
                case WriteOperationStatus.Success:
                    return Models.WriteStatus.Success;
                case WriteOperationStatus.Fail:
                    return Models.WriteStatus.Fail;
                case WriteOperationStatus.Pending:
                    return Models.WriteStatus.Pending;
                case WriteOperationStatus.Unknown:
                default:
                    return Models.WriteStatus.Unknown;
            }
        }

    }
}
