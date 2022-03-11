# DataCore.Adapter.KeyValueStore.FileSystem

Implementation of [IKeyValueStore](/src/DataCore.Adapter.Abstractions/Services/IKeyValueStore.cs) that uses the file system to persist data.

Each key in the store is saved to a different file. The name of the fix is the key, converted to hexadecimal and suffixed with a file extension e.g. `[0x48, 0x65,0x6C, 0x6C]` becomes `48656C6C.data`.


# Example Usage

Register `FileSystemKeyValueStore` with the `IAdapterConfigurationBuilder` when configuring adapter services in the dependency injection container:

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
            var options = new FileSystemKeyValueStoreOptions() {
                Path = Path.Combine(AppContext.BaseDirectory, "Data", "KVStore")
            };

            return ActivatorUtilities.CreateInstance<FileSystemKeyValueStore>(sp, options);
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
