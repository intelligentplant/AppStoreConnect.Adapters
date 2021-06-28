# DataCore.Adapter.KeyValueStore.FASTER

Implementation of [IKeyValueStore](/src/DataCore.Adapter.Abstractions/Services/IKeyValueStore.cs) that uses [Microsoft FASTER](https://microsoft.github.io/FASTER/) to persist data.

The [FasterKeyValueStore](./FasterKeyValueStore.cs) implementation is based on code and concepts from [Jering.KeyValueStore](https://github.com/JeringTech/KeyValueStore). See [here](/THIRD_PARTY_LICENSES) for licence information.


# Example Usage

Register `FasterKeyValueStore` with the `IAdapterConfigurationBuilder` when configuring adapter services in the dependency injection container:

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
            // Configure options for FasterKeyValueStore.

            var fasterLogDirectory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "Data", "FASTER"));
            fasterLogDirectory.Create();

            var options = new FasterKeyValueStoreOptions() {
                // Configures FASTER to use the local file system to store the 
                // part of the KV store that can't be held in memory.
                LogDeviceFactory = () => Devices.CreateLogDevice(fasterLogDirectory.FullName),

                // Configures FASTER to create checkpoints in the same folder 
                // where the on-disk part of the KV store is held. Checkpoints 
                // must be enabled to allow the store to persist and restore 
                // its contents between restarts.
                CheckpointManagerFactory = () => FasterKeyValueStore.CreateLocalStorageCheckpointManager(fasterLogDirectory.FullName),

                // Configures how frequently automatic checkpoints of the 
                // entire log should be taken. Checkpoints are only taken if 
                // the store has been modified since the last checkpoint was 
                // taken. A checkpoint is also created when the store is 
                // disposed if required.
                CheckpointInterval = TimeSpan.FromMinutes(1)
            };

            return ActivatorUtilities.CreateInstance<FasterKeyValueStore>(sp, options);
        })
        .AddAdapter(sp => {
            const string adapterId = "my-adapter-1";
            return ActivatorUtilities.CreateInstance<MyAdapter>(
                sp, 
                adapterId, 
                new MyAdapterOptions()
            );
        });
}
```

The `IKeyValueStore` can then be injected into an adapter in the same was as any other service. If you are hosting multiple adapters, or the `IKeyValueStore` will be used by other parts of your application, it is recommended to pass a scoped `IKeyValueStore` when creating the adapter, to ensure that all items in the store associated with the adapter use the same prefix:

```csharp
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
