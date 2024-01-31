# Adapter Host Logging

> Note: this document assumes that you have used the project template for Visual Studio and `dotnet new` to create an adapter host. See [here](../src/DataCore.Adapter.Templates) for more information.

By default, adapter hosts created using the project template for Visual Studio and `dotnet new` use [Serilog](https://serilog.net/) to write log messages to the console.

The section below describes how to use Serilog to write logs to files in addition to the console. Please refer to the Serilog documentation for full details of how to write logs to additional destinations.


# Configuring Serilog to write to files via `appsettings.json`

Serilog can be configured to write to log files with a simple configuration file modification:

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
            "type": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
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

- Writes log messages to the console using the same default format used by Microsoft's console logger.
- Writes log messages to daily log files in the `logs/` folder under the adapter host's installation directory using the [Compact Log Event Format](https://clef-json.org/) (CLEF). 

By default, log files for the last 31 days are kept.

Refer to the Serilog documentation for full details about configuration of logging destinations!
