﻿using System;
namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Represents the reported status of a health check result.
    /// </summary>
    public enum HealthStatus {

        /// <summary>
        /// Indicates that the health check determined that the component was unhealthy, or an 
        /// unhandled exception was thrown while executing the health check.
        /// </summary>
        Unhealthy = 0,

        /// <summary>
        /// Indicates that the health check determined that the component was in a degraded state.
        /// </summary>
        Degraded = 1,

        /// <summary>
        /// Indicates that the health check determined that the component was healthy.
        /// </summary>
        Healthy = 2

    }
}
