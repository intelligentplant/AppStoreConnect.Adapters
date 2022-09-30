using DataCore.Adapter.RealTimeData;

namespace DataCore.Adapter.Grpc.Proxy.RealTimeData.Features {

    /// <summary>
    /// <see cref="IWriteSnapshotTagValues"/> implementation.
    /// </summary>
    internal partial class WriteSnapshotTagValuesImpl : ProxyAdapterFeature, IWriteSnapshotTagValues {

        /// <summary>
        /// Creates a new <see cref="WriteSnapshotTagValuesImpl"/> instance.
        /// </summary>
        /// <param name="proxy">
        ///   The proxy that owns the instance.
        /// </param>
        public WriteSnapshotTagValuesImpl(GrpcAdapterProxy proxy) : base(proxy) { }

    }

}
