using System.Runtime.CompilerServices;

// DataCore.Adapter.Abstractions is allowed to use internals from this assembly for use in builder
// types etc.
[assembly: InternalsVisibleTo("DataCore.Adapter.Abstractions")]

// DataCore.Adapter.Json is allowed to use internals from this assembly so that it can correctly 
// deserialize Variant instances.
[assembly: InternalsVisibleTo("DataCore.Adapter.Json")]

// Unit tests are allowed to use internals from this assembly.
[assembly: InternalsVisibleTo("DataCore.Adapter.Tests")]
