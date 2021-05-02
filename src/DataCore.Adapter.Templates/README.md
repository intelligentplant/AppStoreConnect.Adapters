# DataCore.Adapter.Templates

This project defines templates for the [dotnet new](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-new) command for creating App Store Connect adapters.


# Installing Templates

You can install the [App Store Connect adapter templates](https://www.nuget.org/packages/IntelligentPlant.AppStoreConnect.Adapter.Templates) for `dotnet new` as follows:

**Latest Version:**

```
dotnet new --install IntelligentPlant.AppStoreConnect.Adapter.Templates
```

> To install a pre-release version, follow the *Specific Version* instructions below.

**Specific Version:**

```
dotnet new --install IntelligentPlant.AppStoreConnect.Adapter.Templates::1.2.3
```

**From Source:**

Alternatively, you can install the template from source by checking out this repository, [building the solution](/build.cake), navigating to the [root templates folder](/src/DataCore.Adapter.Templates) from the command line, and running the following command:

```
dotnet new --install .\
```


# Creating a Hosted Adapter using a Template

To create a new project for an adapter that is hosted using an ASP.NET Core application, run the following commands:

```
mkdir MyNewAdapter
cd MyNewAdapter
dotnet new aschostedadapter
```

This will create a new adapter hosted in an ASP.NET Core application that App Store Connect can connect to using REST API calls, SignalR, or gRPC. The `README.md` file for the new project provides additional instructions for completing the setup.

You can open the project in Visual Studio by double clicking it. In the future, you will be able to [use the template from inside Visual Studio](https://devblogs.microsoft.com/dotnet/net-cli-templates-in-visual-studio/).


## Creating a Visual Studio Solution

The above steps create a `.csproj` file and associated source files that can be compiled and run using the `dotnet` command. To create a Visual Studio solution file containing the project, you can follow these steps instead:

Create solution file:

```
mkdir MyAdapterSolution
cd MyAdapterSolution
dotnet new sln
```

Create project:

```
mkdir MyNewAdapter
cd MyNewAdapter
dotnet new aschostedadapter
```

Add project to solution:

```
cd ..
dotnet sln add ./MyNewAdapter/MyNewAdapter.csproj
```

## Specifying Project Parameters

When creating the project, you can provide command line parameters to pre-populate some project properties. Run `dotnet new aschostedadapter --help` to see all of the available options. 

Example:

```
# Specifies the local HTTPS port to use instead of randomly choosing a port.

dotnet new aschostedadapter --port 43789
```

```
# Specifies adapter metadata.

dotnet new aschostedadapter --adapter-name "My MQTT Adapter" --adapter-description "Adapter for MQTT"
```

```
# Specifies vendor metadata.

dotnet new aschostedadapter --vendor-name "Intelligent Plant" --vendor-url "https://www.intelligentplant.com"
```
