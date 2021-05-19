# Example Hosted Adapter Template

This project is used as the basis of the ASP.NET Core adapter host template for `dotnet new`.

The [README_TEMPLATE.md](./README_TEMPLATE.md) file is used as the `README.md` file in the template.

This project contains a [Directory.Build.props](./Directory.Build.props) file that is used to set the `_AdapterPackageVersion` build property that is used at restore and compile time to allow [ExampleHostedAdapter.csproj](./ExampleHostedAdapter.csproj) to compile correctly. The `Directory.Build.props` file is excluded from the final template built by the [DataCore.Adapter.Templates](/src/DataCore.Adapter.Templates) folder, as the `$(_AdapterPackageVersion)` is replaced with a literal package version in the generated template.

