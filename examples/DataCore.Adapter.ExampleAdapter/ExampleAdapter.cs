﻿using System;
using System.Threading;
using System.Threading.Tasks;
using DataCore.Adapter.Events.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter that has data source capabilities (tag search, tag value queries, etc). The 
    /// adapter contains a set of sensor-like data for 3 tags that it will loop over.
    /// </summary>
    public class ExampleAdapter : Csv.CsvAdapter, IExampleExtensionFeature  {

        /// <summary>
        /// Creates a new <see cref="ExampleAdapter"/> object.
        /// </summary>
        /// <param name="loggerFactory">
        ///   The adapter logger factory.
        /// </param>
        public ExampleAdapter(ILoggerFactory loggerFactory) : base(
            Options.Create(new Csv.CsvAdapterOptions() {
                Id = "example",
                Name = "Example Adapter",
                Description = "An example data source adapter",
                IsDataLoopingAllowed = true,
                SnapshotPushUpdateInterval = 5000,
                GetCsvStream = () => new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(CsvData))
            }),
            loggerFactory
        ) {
            // Register additional features!
            AddFeature<IEventMessagePush, EventsSubscriptionManager>(new EventsSubscriptionManager(TimeSpan.FromSeconds(60)));
        }


        DateTime IExampleExtensionFeature.GetCurrentTime() {
            return DateTime.UtcNow;
        }


        private class EventsSubscriptionManager : Events.Utilities.EventMessageSubscriptionManager {

            private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();


            internal EventsSubscriptionManager(TimeSpan interval) : base(Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance) {
                var startup = DateTime.UtcNow;
                _ = Task.Run(async () => {
                    try {
                        while (!_disposedTokenSource.IsCancellationRequested) {
                            await Task.Delay(interval, _disposedTokenSource.Token).ConfigureAwait(false);
                            OnMessage(Events.Models.EventMessageBuilder
                                .Create()
                                .WithPriority(Events.Models.EventPriority.Low)
                                .WithCategory("System Messages")
                                .WithMessage($"Uptime: {(DateTime.UtcNow - startup)}")
                                .Build()
                            );
                        }
                    }
                    catch { }
                });
            }


            protected override Task OnSubscriptionAdded(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }

            protected override Task OnSubscriptionRemoved(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    _disposedTokenSource.Cancel();
                    _disposedTokenSource.Dispose();
                }
            }

        }


        /// <summary>
        /// CSV data that we parse at startup.
        /// </summary>
        private const string CsvData = @"
TIME,[name=Tag1|id=1|units=deg C],[name=Tag2|id=2|units=deg C],[name=Tag3|id=3|units=deg C],[name=Tag4|id=4|state_NORMAL=0|state_ALARM=1]
2019-03-27T09:50:12Z,55.65,54.81,45.01,0
2019-03-27T09:50:27Z,53.46,55.4,46.89,0
2019-03-27T09:50:42Z,54.79,56.44,46.03,0
2019-03-27T09:50:57Z,56.43,55.72,46.99,0
2019-03-27T09:51:12Z,55.73,56.36,46.57,0
2019-03-27T09:51:27Z,57.31,54.69,48.2,0
2019-03-27T09:51:42Z,57.87,54.86,46.98,0
2019-03-27T09:51:57Z,55.32,56.22,45.59,0
2019-03-27T09:52:12Z,53.02,55.2,46.48,0
2019-03-27T09:52:27Z,55.04,55.75,47.11,1
2019-03-27T09:52:42Z,55.6,54.57,48.87,1
2019-03-27T09:52:57Z,55.77,54.75,47.72,1
2019-03-27T09:53:12Z,55.4,55.18,47.03,1
2019-03-27T09:53:27Z,54.68,57.05,47.56,1
2019-03-27T09:53:42Z,54.83,55.45,46.16,1
2019-03-27T09:53:57Z,55.16,57.02,46.48,0
2019-03-27T09:54:12Z,55.8,57.14,47.56,0
2019-03-27T09:54:27Z,58.26,57.44,48.65,0
2019-03-27T09:54:42Z,60.79,56.55,48.52,0
2019-03-27T09:54:57Z,59.66,55.82,46.78,0
2019-03-27T09:55:12Z,62.11,54.58,47.9,0
2019-03-27T09:55:27Z,61.04,56.42,46.2,0
2019-03-27T09:55:42Z,62.42,57.09,44.88,0
2019-03-27T09:55:57Z,61.16,56.5,45.14,0
2019-03-27T09:56:12Z,62.83,56.32,46.83,0
2019-03-27T09:56:27Z,62.15,54.63,47.44,0
2019-03-27T09:56:42Z,59.62,52.77,45.97,0
2019-03-27T09:56:57Z,59.46,52.96,47.06,0
2019-03-27T09:57:12Z,61.65,54.49,45.58,0
2019-03-27T09:57:27Z,64.19,53.3,46.61,0
2019-03-27T09:57:42Z,62.77,51.66,47.59,0
2019-03-27T09:57:57Z,64.1,52.71,46.07,0
2019-03-27T09:58:12Z,64.96,51.46,47.04,0
2019-03-27T09:58:27Z,64.7,49.83,48.26,0
2019-03-27T09:58:42Z,65.87,49.6,49.77,0
2019-03-27T09:58:57Z,65.83,48.01,51.58,0
2019-03-27T09:59:12Z,68,46.41,52.15,0
2019-03-27T09:59:27Z,66.93,48.17,50.86,0
2019-03-27T09:59:42Z,64.89,49.16,51.38,0
2019-03-27T09:59:57Z,64.41,50.27,49.94,0
2019-03-27T10:00:12Z,64.15,51.36,50.4,0
2019-03-27T10:00:27Z,65.84,53.24,50.28,0
2019-03-27T10:00:42Z,65.93,52.26,50.7,0
2019-03-27T10:00:57Z,63.96,52.72,50.36,0
2019-03-27T10:01:12Z,66.07,52.63,49.68,0
2019-03-27T10:01:27Z,64.05,52.38,50.39,0
2019-03-27T10:01:42Z,63.83,50.9,51.74,1
2019-03-27T10:01:57Z,65.97,51.68,53.16,1
2019-03-27T10:02:12Z,67.45,50.64,54,1
2019-03-27T10:02:27Z,67.59,48.74,52.74,1
2019-03-27T10:02:42Z,69.34,50.07,54.52,1
2019-03-27T10:02:57Z,68.05,49.08,52.94,1
2019-03-27T10:03:12Z,69.08,48.94,54.42,1
2019-03-27T10:03:27Z,69.68,49.8,55.53,1
2019-03-27T10:03:42Z,69.57,50.25,57.01,1
2019-03-27T10:03:57Z,68.18,50.21,55.64,1
2019-03-27T10:04:12Z,69.63,52.05,57.52,1
2019-03-27T10:04:27Z,71.22,53.29,57.32,1
2019-03-27T10:04:42Z,71.41,54.77,55.66,1
2019-03-27T10:04:57Z,69.97,55.69,55.35,1
2019-03-27T10:05:12Z,68.78,57.53,57.18,1
2019-03-27T10:05:27Z,69.81,59.3,56.86,1
2019-03-27T10:05:42Z,68.2,58.7,57.94,1
2019-03-27T10:05:57Z,68.18,58.4,59.59,1
2019-03-27T10:06:12Z,70.59,58.44,58.59,1
2019-03-27T10:06:27Z,69.28,57.88,57.85,1
2019-03-27T10:06:42Z,68.96,58.38,58.66,1
2019-03-27T10:06:57Z,67.59,57.07,57.41,1
2019-03-27T10:07:12Z,68.01,57.42,58.08,1
2019-03-27T10:07:27Z,68.45,57.92,59.36,0
2019-03-27T10:07:42Z,66.58,58.12,59.59,0
2019-03-27T10:07:57Z,64.18,58.2,58.59,0
2019-03-27T10:08:12Z,64.88,57.53,58.55,0
2019-03-27T10:08:27Z,64.41,56.71,57.08,0
2019-03-27T10:08:42Z,66.56,56.54,57.85,0
2019-03-27T10:08:57Z,65.97,55.14,57.93,0
2019-03-27T10:09:12Z,66.3,55.82,56.74,0
2019-03-27T10:09:27Z,68.22,56.29,57.79,0
2019-03-27T10:09:42Z,65.92,57.36,58.03,0
2019-03-27T10:09:57Z,65.5,59.09,58.99,0
2019-03-27T10:10:12Z,67.46,60.36,58.82,0
2019-03-27T10:10:27Z,68.52,61.05,58.07,0
2019-03-27T10:10:42Z,66.17,60.54,57.64,0
2019-03-27T10:10:57Z,65.75,60.18,57.97,0
2019-03-27T10:11:12Z,65.29,61.63,56.83,0
2019-03-27T10:11:27Z,65.95,60.69,56.07,0
2019-03-27T10:11:42Z,65.68,59.39,54.89,0
2019-03-27T10:11:57Z,67.66,58.93,53.27,0
2019-03-27T10:12:12Z,68.92,58.68,54.18,0
2019-03-27T10:12:27Z,69.51,57.02,53.18,0
2019-03-27T10:12:42Z,67.12,58.37,54.12,0
2019-03-27T10:12:57Z,65.67,60.02,53.48,0
2019-03-27T10:13:12Z,65.67,61.42,52.83,0
2019-03-27T10:13:27Z,64.44,61.25,52.61,0
2019-03-27T10:13:42Z,64.73,60.23,52.68,0
2019-03-27T10:13:57Z,64.85,60.2,54.06,0
2019-03-27T10:14:12Z,63.73,59.6,54.61,0
2019-03-27T10:14:27Z,63.86,58.66,55.73,0
2019-03-27T10:14:42Z,62.81,58.66,54.73,0
2019-03-27T10:14:57Z,62.21,57.63,54.69,0
2019-03-27T10:15:12Z,63.26,57.13,56.42,0
2019-03-27T10:15:27Z,61.61,57.69,54.96,0
2019-03-27T10:15:42Z,59.66,57.67,53.6,0
2019-03-27T10:15:57Z,61.1,57.44,53.57,0
2019-03-27T10:16:12Z,59.28,56.1,55.21,0
2019-03-27T10:16:27Z,60.35,56.01,55,0
2019-03-27T10:16:42Z,59.81,55.4,54.86,0
2019-03-27T10:16:57Z,59.04,54.4,53.18,0
2019-03-27T10:17:12Z,56.86,52.92,53.84,0
2019-03-27T10:17:27Z,55.55,53.06,52.22,0
2019-03-27T10:17:42Z,56.61,53.09,52.68,0
2019-03-27T10:17:57Z,54.96,54.58,54.36,0
2019-03-27T10:18:12Z,52.73,54.4,54.2,0
2019-03-27T10:18:27Z,54.62,56.03,53.3,0
2019-03-27T10:18:42Z,56.04,54.89,54.36,0
2019-03-27T10:18:57Z,54.9,54.88,54.42,0
2019-03-27T10:19:12Z,53.57,54.21,52.65,0
2019-03-27T10:19:27Z,51.44,53.87,52.81,0
2019-03-27T10:19:42Z,49.5,55.14,51.19,0
2019-03-27T10:19:57Z,50.65,53.69,51.41,0
2019-03-27T10:20:12Z,50.09,54.35,50.94,0
2019-03-27T10:20:27Z,50.54,53.07,51.56,0
2019-03-27T10:20:42Z,51.75,53.35,51.08,0
2019-03-27T10:20:57Z,52.49,54.34,50.05,0
2019-03-27T10:21:12Z,51.17,55.95,50.75,0
2019-03-27T10:21:27Z,50.14,56.28,52.64,0
2019-03-27T10:21:42Z,49.2,57.42,52.1,0
2019-03-27T10:21:57Z,51.6,57.32,50.69,0
2019-03-27T10:22:12Z,51.86,55.51,52.47,0
2019-03-27T10:22:27Z,54.25,56.27,51.96,0
2019-03-27T10:22:42Z,55.3,55.5,51.63,0
2019-03-27T10:22:57Z,54.89,56.95,52.97,0
2019-03-27T10:23:12Z,53.19,55.28,52.02,0
2019-03-27T10:23:27Z,53.23,53.81,52.53,0
2019-03-27T10:23:42Z,54.58,51.98,51.01,0
2019-03-27T10:23:57Z,54.45,53.87,51.81,0
2019-03-27T10:24:12Z,55.72,53.73,50.64,0
2019-03-27T10:24:27Z,58,55.02,49.23,0
2019-03-27T10:24:42Z,55.62,56.79,50.66,0
2019-03-27T10:24:57Z,55.2,57.72,50.27,0
2019-03-27T10:25:12Z,56.81,59.36,51.17,0
2019-03-27T10:25:27Z,54.44,60.44,52.01,0
2019-03-27T10:25:42Z,54.77,59.76,51.94,0
2019-03-27T10:25:57Z,54.7,59.07,52.54,0
2019-03-27T10:26:12Z,55.69,59.05,51.58,0
2019-03-27T10:26:27Z,54.45,59.69,52.16,0
2019-03-27T10:26:42Z,53.6,59.01,53.08,0
2019-03-27T10:26:57Z,54.49,60.85,51.56,0
2019-03-27T10:27:12Z,53.14,60.3,52.26,0
2019-03-27T10:27:27Z,54.08,59.98,51.29,0
2019-03-27T10:27:42Z,55.56,59.11,50.03,0
2019-03-27T10:27:57Z,55.1,57.92,49.18,0
2019-03-27T10:28:12Z,55.35,56.72,49.11,0
2019-03-27T10:28:27Z,55.75,57.65,48.53,0
2019-03-27T10:28:42Z,55.32,57.84,49,0
2019-03-27T10:28:57Z,54,57.2,48.14,0
2019-03-27T10:29:12Z,55.06,55.37,47.65,0
2019-03-27T10:29:27Z,56.94,55.25,45.76,0
2019-03-27T10:29:42Z,57.83,53.89,46.24,0
2019-03-27T10:29:57Z,56.32,53.45,45.6,0
2019-03-27T10:30:12Z,53.88,53.8,44.22,0
2019-03-27T10:30:27Z,51.44,52.22,43.12,0
2019-03-27T10:30:42Z,50.14,52.7,43.21,0
2019-03-27T10:30:57Z,51.45,52.74,43.11,0
2019-03-27T10:31:12Z,51.31,52.14,43.69,0
2019-03-27T10:31:27Z,48.93,52.11,44.87,0
2019-03-27T10:31:42Z,47.13,50.72,44.06,0
2019-03-27T10:31:57Z,45,52.61,43.37,0
2019-03-27T10:32:12Z,45.67,53.91,41.75,0
2019-03-27T10:32:27Z,46.18,55.28,42.21,0
2019-03-27T10:32:42Z,44.69,53.44,43.86,0
2019-03-27T10:32:57Z,43.87,53.79,41.98,0
2019-03-27T10:33:12Z,45.11,53.94,41.45,0
2019-03-27T10:33:27Z,45.46,55.53,39.61,0
2019-03-27T10:33:42Z,43.15,53.8,39.5,0
2019-03-27T10:33:57Z,40.81,55.21,39.01,0
2019-03-27T10:34:12Z,39.34,55.2,39.15,0
2019-03-27T10:34:27Z,37.27,54.16,40.89,0
2019-03-27T10:34:42Z,36.44,53.64,39.27,0
2019-03-27T10:34:57Z,34.23,52.67,39.47,0
2019-03-27T10:35:12Z,32.29,53.9,39.23,0
2019-03-27T10:35:27Z,29.77,55.74,41.06,0
2019-03-27T10:35:42Z,29.47,55.56,40.2,0
2019-03-27T10:35:57Z,27,55.91,41.33,0
2019-03-27T10:36:12Z,25.81,56.15,41.9,0
2019-03-27T10:36:27Z,26.87,57.54,43.44,0
2019-03-27T10:36:42Z,24.86,58.06,44.84,0
2019-03-27T10:36:57Z,25.24,59.89,45.72,0
2019-03-27T10:37:12Z,24.08,58.54,45.06,0
2019-03-27T10:37:27Z,22.09,59.02,43.66,0
2019-03-27T10:37:42Z,20.98,58.84,45.25,0
2019-03-27T10:37:57Z,20.86,60.52,44.49,0
2019-03-27T10:38:12Z,23.24,61.38,43.87,0
2019-03-27T10:38:27Z,23.69,60.47,44.24,0
2019-03-27T10:38:42Z,21.74,60.79,44.67,0
2019-03-27T10:38:57Z,23.75,62.14,43.8,0
2019-03-27T10:39:12Z,25.31,61.53,43.67,0
2019-03-27T10:39:27Z,23.71,59.73,42.27,0
2019-03-27T10:39:42Z,24.71,59.31,43.35,0
2019-03-27T10:39:57Z,22.3,58.52,41.75,0
2019-03-27T10:40:12Z,22.2,58.84,41,0
2019-03-27T10:40:27Z,23.15,57.45,40.95,0
2019-03-27T10:40:42Z,21.8,58.66,40.24,0
2019-03-27T10:40:57Z,21.29,60.16,39.89,0
2019-03-27T10:41:12Z,20.29,62.04,41.58,0
2019-03-27T10:41:27Z,22.46,60.46,42.33,0
2019-03-27T10:41:42Z,21.7,61.16,42.34,0
2019-03-27T10:41:57Z,21.22,61.35,42.3,0
2019-03-27T10:42:12Z,22.14,61.58,43.31,0
2019-03-27T10:42:27Z,23.98,62.7,45.05,0
2019-03-27T10:42:42Z,22.69,61.4,45.51,0
2019-03-27T10:42:57Z,25.14,61.78,45.73,0
2019-03-27T10:43:12Z,26.72,62.03,45.59,0
2019-03-27T10:43:27Z,28.16,60.25,47.44,0
2019-03-27T10:43:42Z,29.18,62.04,45.66,0
2019-03-27T10:43:57Z,29.79,62.15,45.62,0
2019-03-27T10:44:12Z,31.46,61.84,47.12,0
2019-03-27T10:44:27Z,30.35,61.86,46.82,0
2019-03-27T10:44:42Z,29.79,60.72,45.86,0
2019-03-27T10:44:57Z,30.5,59.18,47.65,0
2019-03-27T10:45:12Z,29.45,57.56,45.86,0
2019-03-27T10:45:27Z,29.95,57.08,45.89,0
2019-03-27T10:45:42Z,31.03,55.33,47.66,0
2019-03-27T10:45:57Z,28.53,54.03,47.97,0
2019-03-27T10:46:12Z,27.78,54.2,49.34,0
2019-03-27T10:46:27Z,28.99,54.89,48.84,0
2019-03-27T10:46:42Z,26.58,55.43,48.89,0
2019-03-27T10:46:57Z,27.68,54.71,48.35,0
2019-03-27T10:47:12Z,27.45,53.77,48.03,0
2019-03-27T10:47:27Z,26.02,55.66,49.27,0
2019-03-27T10:47:42Z,23.74,53.95,50.8,0
2019-03-27T10:47:57Z,24.88,54.83,49.53,0
2019-03-27T10:48:12Z,22.69,54.41,49.27,0
2019-03-27T10:48:27Z,22.56,53.25,50.94,0
2019-03-27T10:48:42Z,22.56,51.92,52.01,0
2019-03-27T10:48:57Z,21.14,53.34,51.33,0
2019-03-27T10:49:12Z,23.65,55.18,52.02,0
2019-03-27T10:49:27Z,25.42,55.07,52.71,0
2019-03-27T10:49:42Z,27.76,55.76,52.5,1
2019-03-27T10:49:57Z,25.75,54.14,52.71,1
2019-03-27T10:50:12Z,27.84,52.86,52.83,1";

    }

}
