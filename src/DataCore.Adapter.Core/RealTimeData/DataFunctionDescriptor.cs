using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a data function that is supported when making a call for processed historical data.
    /// </summary>
    public sealed class DataFunctionDescriptor {

        /// <summary>
        /// The function ID.
        /// </summary>
        [Required]
        public string Id { get; }

        /// <summary>
        /// The function display name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        /// The function description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The method used to compute the sample time for a calculated value.
        /// </summary>
        public DataFunctionSampleTimeType SampleTime { get; }

        /// <summary>
        /// The method used to compute the quality status for a calculated value.
        /// </summary>
        public DataFunctionStatusType Status { get; }

        /// <summary>
        /// Bespoke properties associated with the data function.
        /// </summary>
        public IEnumerable<AdapterProperty> Properties { get; }


        /// <summary>
        /// Creates a new <see cref="DataFunctionDescriptor"/> object.
        /// </summary>
        /// <param name="id">
        ///   The function ID.
        /// </param>
        /// <param name="name">
        ///   The function name.
        /// </param>
        /// <param name="description">
        ///   The function description.
        /// </param>
        /// <param name="sampleTime">
        ///   The sample time calculation method for the function. When <see cref="DataFunctionStatusType.Custom"/>
        ///   is specified, a property in the <paramref name="properties"/> collection should 
        ///   describe how the sample time is calculated.
        /// </param>
        /// <param name="status">
        ///   The quality status calculation method for the function. When <see cref="DataFunctionStatusType.Custom"/>
        ///   is specified, a property in the <paramref name="properties"/> collection should 
        ///   describe how the quality status is calculated.
        /// </param>
        /// <param name="properties">
        ///   Additional properties associated with the function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        public DataFunctionDescriptor(
            string id, 
            string name, 
            string description, 
            DataFunctionSampleTimeType sampleTime = DataFunctionSampleTimeType.Unspecified, 
            DataFunctionStatusType status = DataFunctionStatusType.Unspecified, 
            IEnumerable<AdapterProperty> properties = null
        ) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = string.IsNullOrWhiteSpace(name) ? id : name;
            Description = description;
            SampleTime = sampleTime;
            Status = status;
            Properties = properties?.ToArray() ?? Array.Empty<AdapterProperty>();
        }


        /// <summary>
        /// Creates a new <see cref="DataFunctionDescriptor"/> object.
        /// </summary>
        /// <param name="id">
        ///   The function ID.
        /// </param>
        /// <param name="name">
        ///   The function name.
        /// </param>
        /// <param name="description">
        ///   The function description.
        /// </param>
        /// <param name="sampleTime">
        ///   The sample time calculation method for the function. When <see cref="DataFunctionStatusType.Custom"/>
        ///   is specified, a property in the <paramref name="properties"/> collection should 
        ///   describe how the sample time is calculated.
        /// </param>
        /// <param name="status">
        ///   The quality status calculation method for the function. When <see cref="DataFunctionStatusType.Custom"/>
        ///   is specified, a property in the <paramref name="properties"/> collection should 
        ///   describe how the quality status is calculated.
        /// </param>
        /// <param name="properties">
        ///   Additional properties associated with the function.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        public static DataFunctionDescriptor Create(
            string id, 
            string name = null, 
            string description = null, 
            DataFunctionSampleTimeType sampleTime = DataFunctionSampleTimeType.Unspecified, 
            DataFunctionStatusType status = DataFunctionStatusType.Unspecified, 
            IEnumerable<AdapterProperty> properties = null
        ) {
            return new DataFunctionDescriptor(id, name ?? id, description, sampleTime, status, properties);
        }

    }
}
