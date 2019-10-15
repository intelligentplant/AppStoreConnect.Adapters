using DataCore.Adapter.Grpc;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// Extension methods for converting from gRPC types to their adapter equivalents.
    /// </summary>
    public static class GrpcCommonExtensions {

        public static HostInfo ToAdapterHostInfo(this Grpc.HostInfo hostInfo) {
            if (hostInfo == null) {
                return null;
            }

            return HostInfo.Create(
                hostInfo.Name,
                hostInfo.Description,
                hostInfo.Version,
                hostInfo.VendorInfo == null 
                    ? null 
                    : VendorInfo.Create(hostInfo.VendorInfo.Name, hostInfo.VendorInfo.Url),
                hostInfo.Properties
            );
        }


        public static AdapterDescriptor ToAdapterDescriptor(this Grpc.AdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return AdapterDescriptor.Create(descriptor.Id, descriptor.Name, descriptor.Description);
        }


        public static AdapterDescriptorExtended ToExtendedAdapterDescriptor(this Grpc.ExtendedAdapterDescriptor descriptor) {
            if (descriptor == null) {
                return null;
            }

            return AdapterDescriptorExtended.Create(
                descriptor.AdapterDescriptor?.Id,
                descriptor.AdapterDescriptor?.Name,
                descriptor.AdapterDescriptor?.Description,
                descriptor.Features,
                descriptor.Extensions,
                descriptor.Properties
            );
        }


        public static WriteStatus ToAdapterWriteStatus(this Grpc.WriteOperationStatus status) {
            switch (status) {
                case WriteOperationStatus.Success:
                    return WriteStatus.Success;
                case WriteOperationStatus.Fail:
                    return WriteStatus.Fail;
                case WriteOperationStatus.Pending:
                    return WriteStatus.Pending;
                case WriteOperationStatus.Unknown:
                default:
                    return WriteStatus.Unknown;
            }
        }

    }
}
