namespace DataCore.Adapter.Grpc.Proxy.Common {
    internal static class CommonExtensions {

        internal static Adapter.Common.Models.WriteStatus ToAdapterWriteStatus(this WriteOperationStatus status) {
            switch (status) {
                case WriteOperationStatus.Success:
                    return Adapter.Common.Models.WriteStatus.Success;
                case WriteOperationStatus.Fail:
                    return Adapter.Common.Models.WriteStatus.Fail;
                case WriteOperationStatus.Pending:
                    return Adapter.Common.Models.WriteStatus.Pending;
                case WriteOperationStatus.Unknown:
                default:
                    return Adapter.Common.Models.WriteStatus.Unknown;
            }
        }

    }
}
