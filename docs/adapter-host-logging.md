# Adapter Host Telemetry and Observability

> Note: this document assumes that you have used the project template for Visual Studio and `dotnet new` to create an adapter host. See [here](../src/DataCore.Adapter.Templates) for more information.

The adapter host uses OpenTelemetry to observe traces, metrics and logs generated by the application. An OpenTelemetry Protocol (OTLP) exporter that exports traces is enabled by default. You can disable the exporter, modify the signals to be exported, or configure the OTLP collector endpoint using the `appsettings.json` file.

See [here](https://github.com/wazzamatazz/opentelemetry-extensions) for more information about configuring the OTLP exporter.

Additionally, metrics can be scraped from the application's `/metrics` endpoint in Prometheus format. The Prometheus endpoint can be disabled or removed from the `Program.cs` file if not required.

The adapter host uses the Microsoft.Extensions.Logging library for logging. If required, you can add additional logging providers to the application by modifying the `Program.cs` file.


