﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

using DataCore.Adapter.Common;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Base class for requests that query tags for data.
    /// </summary>
    public abstract class ReadTagDataRequest : AdapterRequest {

        /// <summary>
        /// The tag names or IDs to query.
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(100)]
        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">
        ///   The validation context.
        /// </param>
        /// <returns>
        ///   A collection of validation errors.
        /// </returns>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (Tags != null) {
                if (Tags.Any(x => x == null)) {
                    yield return new ValidationResult(SharedResources.Error_NameOrIdCannotBeNull, new[] { nameof(Tags) });
                }
                if (Tags.Any(x => x?.Length > 500)) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_CollectionItemIsTooLong, 500), new[] { nameof(Tags) });
                }
            }
        }

    }

}
