// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace DataCore.Adapter.Diagnostics {

    /// <summary>
    /// Value type stopwatch for diagnostic measurement.
    /// </summary>
    public struct ValueStopwatch {

        /// <summary>
        /// Conversion factor between <see cref="Stopwatch"/> and <see cref="TimeSpan"/>.
        /// </summary>
        private static readonly double s_timestampToTicks = TimeSpan.TicksPerSecond / (double) Stopwatch.Frequency;

        /// <summary>
        /// The timestamp when the <see cref="ValueStopwatch"/> was created.
        /// </summary>
        private readonly long _startTimestamp;

        /// <summary>
        /// Indicates if the stopwatch is active.
        /// </summary>
        /// <remarks>
        ///   <see cref="IsActive"/> will always be false for the <c>default</c> 
        ///   <see cref="ValueStopwatch"/> instance.
        /// </remarks>
        public bool IsActive => _startTimestamp != 0;


        /// <summary>
        /// Creates a new <see cref="ValueStopwatch"/> instance.
        /// </summary>
        /// <param name="startTimestamp">
        ///   The starting timestamp for the stopwatch.
        /// </param>
        private ValueStopwatch(long startTimestamp) {
            _startTimestamp = startTimestamp;
        }


        /// <summary>
        /// Creates a new <see cref="ValueStopwatch"/> instance.
        /// </summary>
        /// <returns>
        ///   A new <see cref="ValueStopwatch"/> instance.
        /// </returns>
        public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());


        /// <summary>
        /// Gets the elapsed time since the stopwatch started.
        /// </summary>
        /// <returns>
        ///   The elapsed time since the <see cref="ValueStopwatch"/> was created.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   <see cref="IsActive"/> is <see langword="false"/>.
        /// </exception>
        public TimeSpan GetElapsedTime() {
            // Start timestamp can't be zero in an initialized ValueStopwatch. It would have to be literally the first thing executed when the machine boots to be 0.
            // So it being 0 is a clear indication of default(ValueStopwatch)
            if (!IsActive) {
                throw new InvalidOperationException(AbstractionsResources.Error_ValueStopwatch_NotInitialised);
            }

            var end = Stopwatch.GetTimestamp();
            var timestampDelta = end - _startTimestamp;
            var ticks = (long) (s_timestampToTicks * timestampDelta);
            return new TimeSpan(ticks);
        }

    }
}
