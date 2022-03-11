# DataCore.Adapter.KeyValueStore.Sqlite

Implementation of [IKeyValueStore](/src/DataCore.Adapter.Abstractions/Services/IKeyValueStore.cs) that uses a Sqlite database to persist data.

All key-value pairs are saved to a table in the database named `kvstore`. The table is created if it does not already exist.


# Example Usage

Register `SqliteKeyValueStore` with the `IAdapterConfigurationBuilder` when configuring adapter services in the dependency injection container:

```csharp
public void ConfigureAdapters(IServiceCollection services) {
    services
        .AddDataCoreAdapterAspNetCoreServices()
        .AddHostInfo(HostInfo.Create(
            "My Host",
            "A brief description of the hosting application",
            "0.9.0-alpha", // SemVer v2
            VendorInfo.Create("Intelligent Plant", "https://appstore.intelligentplant.com"),
            AdapterProperty.Create("Project URL", "https://github.com/intelligentplant/AppStoreConnect.Adapters")
        ))
        .AddKeyValueStore(sp => {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "kvstore.db");
            var options = new SqliteKeyValueStoreOptions() {
                ConnectionString = $"Data Source={path};Cache=Shared"
            };

            return ActivatorUtilities.CreateInstance<SqliteKeyValueStore>(sp, options);
        })
        .AddAdapter(sp => {
            const string adapterId = "my-adapter-1";
            // Assume that MyAdapter's constructor expects an IKeyValueStore instance. 
            return ActivatorUtilities.CreateInstance<MyAdapter>(
                sp, 
                adapterId, 
                new MyAdapterOptions()
            );
        });
}
```

The `IKeyValueStore` can then be injected into an adapter in the same way as any other service. If you are hosting multiple adapters in the same application, or the `IKeyValueStore` will be used by other parts of your application, it is recommended to pass a scoped `IKeyValueStore` when creating the adapter, to ensure that all items in the store associated with the adapter use a common prefix:

```csharp
public void ConfigureAdapters(IServiceCollection services) {
    services
        .AddDataCoreAdapterAspNetCoreServices()
        .AddHostInfo(HostInfo.Create(
            "My Host",
            "A brief description of the hosting application",
            "0.9.0-alpha", // SemVer v2
            VendorInfo.Create("Intelligent Plant", "https://appstore.intelligentplant.com"),
            AdapterProperty.Create("Project URL", "https://github.com/intelligentplant/AppStoreConnect.Adapters")
        ))
        .AddKeyValueStore(sp => {
            // Configuration removed for brevity
        })
        .AddAdapter(sp => {
            const string adapterId = "my-adapter-1";
            var kvStore = sp.GetRequiredService<IKeyValueStore>();

            return ActivatorUtilities.CreateInstance<MyAdapter>(
                sp, 
                adapterId, 
                new MyAdapterOptions(), 
                kvStore.CreateScopedStore(adapterId)
            );
        });
}
```
