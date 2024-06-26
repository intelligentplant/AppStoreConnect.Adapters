using System;

using DataCore.Adapter.Common;
using DataCore.Adapter.RealTimeData.Utilities;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Extension methods for <see cref="TagValueBuilder"/>.
    /// </summary>
    public static class TagValueBuilderExtensions {

        /// <summary>
        /// Sets the value for the sample using a digital state definition.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="TagValueBuilder"/>.
        /// </param>
        /// <param name="state">
        ///   The digital state definition.
        /// </param>
        /// <returns>
        ///   The <see cref="TagValueBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        public static TagValueBuilder WithDigitalStateValue(this TagValueBuilder builder, Tags.DigitalState state) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }

            if (state == null) {
                return builder;
            }

            return builder.WithValue(state.Value, state.Name)
                .WithSteppedTransition(true);
        }


        /// <summary>
        /// Specifies if a tag value represents a stepped value transition (such as a discrete 
        /// value change on a digital tag or an analogue process limit). 
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="TagValueBuilder"/>.
        /// </param>
        /// <param name="stepped">
        ///   <see langword="true"/> if the tag value represents a stepped value transition, or 
        ///   <see langword="false"/> otherwise.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="builder"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// 
        /// <para>
        ///   Calling <see cref="WithSteppedTransition"/> sets the <see cref="WellKnownProperties.TagValue.Stepped"/> 
        ///   property on the tag value.
        /// </para>
        /// 
        /// <para>
        ///   Consuming applications can use this property as a hint regarding how the time series 
        ///   data should be visualized.
        /// </para>
        /// 
        /// </remarks>
        public static TagValueBuilder WithSteppedTransition(this TagValueBuilder builder, bool stepped) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            return builder.WithProperty(WellKnownProperties.TagValue.Stepped.InternToStringCache(), stepped);
        }


        /// <summary>
        /// Adds a set of properties to the tag value being calculated from a bucket.
        /// </summary>
        /// <param name="builder">
        ///   The <see cref="TagValueBuilder"/>.
        /// </param>
        /// <param name="bucket">
        ///   The bucket.
        /// </param>
        /// <returns>
        ///   The updated <see cref="TagValueBuilder"/>.
        /// </returns>
        internal static TagValueBuilder WithBucketProperties(this TagValueBuilder builder, TagValueBucket bucket) {
            if (bucket != null) {
                return builder.WithProperties(
                    new AdapterProperty(CommonTagValuePropertyNames.BucketStart.InternToStringCache(), bucket.UtcBucketStart),
                    new AdapterProperty(CommonTagValuePropertyNames.BucketEnd.InternToStringCache(), bucket.UtcBucketEnd)
                );
            }

            return builder;
        }

    }
}
