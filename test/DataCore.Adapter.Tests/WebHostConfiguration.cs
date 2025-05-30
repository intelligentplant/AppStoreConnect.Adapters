﻿#if NETCOREAPP
using System;
using System.Net.Http;

using GrpcNet = Grpc.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataCore.Adapter.Tests {
    internal static class WebHostConfiguration {

        public const string DefaultHostName = "localhost";

        public static int DefaultPortNumber { get; } = Random.Shared.Next(31415, 31500);

        public static string DefaultUrl { get; } = string.Concat("https://", DefaultHostName, ":", DefaultPortNumber);

        public const string AdapterId = AssemblyInitializer.AdapterId;

        public const string TestTagId = AssemblyInitializer.TestTagId;

        public const string HttpClientName = "AdapterHttpClient";


        internal static void AllowUntrustedCertificates(HttpMessageHandler handler) {
            // For unit test purposes, allow all SSL certificates.
            if (handler is SocketsHttpHandler socketsHandler) {
                socketsHandler.SslOptions.RemoteCertificateValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
                return;
            }
            if (handler is HttpClientHandler clientHandler) {
                clientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;
            }
        }


        public static void ConfigureDefaultServices(IServiceCollection services) {
            services.AddLogging(options => {
                options.AddConsole();
                options.AddDebug();
                options.SetMinimumLevel(LogLevel.Trace);
            });

            services.AddHttpClient<Http.Client.AdapterHttpClient>(HttpClientName).ConfigureHttpMessageHandlerBuilder(builder => {
                AllowUntrustedCertificates(builder.PrimaryHandler);
            }).ConfigureHttpClient(client => {
                client.BaseAddress = new Uri(DefaultUrl + "/");
            });
            
            services.AddTransient(sp => {
                return GrpcNet.Client.GrpcChannel.ForAddress(DefaultUrl, new GrpcNet.Client.GrpcChannelOptions() {
                    HttpClient = sp.GetService<IHttpClientFactory>().CreateClient(HttpClientName)
                });
            });
        }

    }
}
#endif
