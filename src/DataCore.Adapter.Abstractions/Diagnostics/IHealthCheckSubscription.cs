namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Describes a subscription for receiving health check updates as push notifications.
    /// </summary>
    public interface IHealthCheckSubscription : IAdapterSubscription<HealthCheckResult> { }

}
