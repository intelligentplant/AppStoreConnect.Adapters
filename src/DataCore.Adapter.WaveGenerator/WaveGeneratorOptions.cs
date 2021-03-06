﻿using System;

namespace DataCore.Adapter.WaveGenerator {

    /// <summary>
    /// Options for a wave generator function.
    /// </summary>
    public class WaveGeneratorOptions {

        /// <summary>
        /// The display name for the generator.
        /// </summary>
        internal string? Name { get; set; }

        /// <summary>
        /// The description for the generator.
        /// </summary>
        internal string? Description { get; set; }

        /// <summary>
        /// The wave type.
        /// </summary>
        public WaveType Type { get; set; } = WaveType.Sinusoid;

        /// <summary>
        /// The period of the wave, in seconds.
        /// </summary>
        public double Period { get; set; } = TimeSpan.FromHours(1).TotalSeconds;

        /// <summary>
        /// The amplitude of the wave.
        /// </summary>
        public double Amplitude { get; set; } = 1;

        /// <summary>
        /// The phase offset from the base function for the wave type, in seconds.
        /// </summary>
        public double Phase { get; set; }

        /// <summary>
        /// The offset to apply to values generated by the wave function.
        /// </summary>
        public double Offset { get; set; }


        /// <inheritdoc/>
        public override string ToString() {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(Name)) {
                sb.Append(nameof(Name));
                sb.Append('=');
                sb.Append(Name);
                sb.Append(';');
            }

            sb.Append(string.Concat(
                nameof(Type), "=", Type,
                ";",
                nameof(Period), "=", Period,
                ";",
                nameof(Amplitude), "=", Amplitude,
                ";",
                nameof(Phase), "=", Phase,
                ";",
                nameof(Offset), "=", Offset
            ));

            return sb.ToString();
        }

    }
}
