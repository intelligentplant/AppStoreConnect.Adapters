# Adapter Host Logging

> Note: this document assumes that you have used the project template for Visual Studio and `dotnet new` to create an adapter host. See [here](../src/DataCore.Adapter.Templates) for more information.

By default, adapter hosts created using the project template for Visual Studio and `dotnet new` use the built-in `ILogger` implementation from Microsoft to write log messages to the console. This is typically sufficient for local development and debugging scenarios, but you may have a need to use a more sophisticated logging component such as [Serilog](https://serilog.net/) or [NLog](https://nlog-project.org/).

The remainder of this document describes how to use Serilog to write logs to the console and to files. Please refer to the Serilog documentation for full details of how to write logs to additional destinations.


# Installing Serilog

To install Serilog, add the following packages to your adapter host project:

- [Serilog.AspNetCore](https://www.nuget.org/packages/Serilog.AspNetCore/)
- [Serilog.Expressions](https://www.nuget.org/packages/Serilog.Expressions/)
- [Serilog.Settings.Configuration](https://www.nuget.org/packages/Serilog.Settings.Configuration/)


# Modify Adapter Host to Use Serilog

Update your adapter host's `Program.cs` file to register Serilog with the dependency injection container:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());
```

The above code reads logging configuration from the `Serilog` section of the application's `IConfiguration` object.


# Add Serilog Configuration to `appsettings.json`

Remove the `Logging` section from `appsettings.json` (and your `appsettings.{Environment}.json` files if applicable).

Add a `Serilog` section to the configuration file in its place:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "./logs/log-.txt",
          "rollingInterval": "Day",
          "formatter": {
            "type": "Serilog.Templates.ExpressionTemplate, Serilog.Expressions",
            "template": "{@t:yyyy-MM-ddTHH:mm:ss.fff} {@l:u4} {SourceContext}[{Coalesce(EventId.Id, '0')}]  {@m}\n{@x}"
          }
        }
      },
      {
        "Name": "Console",
        "Args": {
          "formatter": {
            "type": "Serilog.Templates.ExpressionTemplate, Serilog.Expressions",
            "template": "{@l:w4}: {SourceContext}[{Coalesce(EventId.Id, '0')}] \n      {@m}\n{@x}",
            "theme": "Serilog.Templates.Themes.TemplateTheme::Code, Serilog.Expressions"
          }
        }
      }
    ]
  }
}
```

The above example performs the following actions:

- Configures the logging levels to match those in the default `Logging` configuration section provided by Microsoft.
- Writes log messages to the console using the same default format used by Microsoft's console logger.
- Writes log messages to daily log files in the `logs/` folder under the adapter host's installation directory. By default, logs for the last 31 days are kept.

Refer to the Serilog documentation for full details about configuration of logging destinations!
