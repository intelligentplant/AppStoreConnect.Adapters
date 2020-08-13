namespace DataCore.Adapter {
    /// <summary>
    /// Interface that all non-standard adapter features must implement.
    /// </summary>
#pragma warning disable CA1040 // Avoid empty interfaces
    public interface IAdapterExtensionFeature : IAdapterFeature { }
#pragma warning restore CA1040 // Avoid empty interfaces
}
