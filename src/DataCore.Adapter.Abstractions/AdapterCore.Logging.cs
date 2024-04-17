using System;

using Microsoft.Extensions.Logging;

namespace DataCore.Adapter {
    partial class AdapterCore {

        [LoggerMessage(1, LogLevel.Warning, "Adapter '{id}' is disabled.")]
        static partial void LogAdapterDisabled(ILogger logger, string id);

        [LoggerMessage(2, LogLevel.Information, "Adapter '{id}' is starting.")]
        static partial void LogAdapterStarting(ILogger logger, string id);

        [LoggerMessage(3, LogLevel.Information, "Adapter '{id}' has started.")]
        static partial void LogAdapterStarted(ILogger logger, string id);

        [LoggerMessage(4, LogLevel.Information, "Adapter '{id}' is stopping.")]
        static partial void LogAdapterStopping(ILogger logger, string id);

        [LoggerMessage(5, LogLevel.Information, "Adapter '{id}' has stopped.")]
        static partial void LogAdapterStopped(ILogger logger, string id);

        [LoggerMessage(6, LogLevel.Warning, "Unable to create wrapper for feature type '{feature}' on adapter '{id}'. Automatic validation and telemetry for this feature will not be available.")]
        static partial void LogUnableToCreateFeatureWrapper(ILogger logger, string feature, string id);

        [LoggerMessage(7, LogLevel.Error, "Error while disposing of feature '{feature}' on adapter '{id}'.")]
        static partial void LogErrorWhileDisposingFeature(ILogger logger, Exception e, object feature, string id);

    }
}
