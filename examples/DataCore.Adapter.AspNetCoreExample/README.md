An example ASP.NET Core 3.0 web application that hosts an App Store Connect adapter for an in-memory data source. A looping data set is used to serve up sensor-like data.

The application exposes the adapter API via MVC API controllers, SignalR, and gRPC services. An SSL certificate is required to be able to serve the HTTP/2 protocol required by gRPC (the self-signed ASP.NET Core development certificate is acceptable for this purpose).

Since the project hosts gRPC services, it must run using Kestrel rather than IIS, since IIS does not support the trailing headers feature of HTTP/2 required by gRPC at the time or writing (October 2019).