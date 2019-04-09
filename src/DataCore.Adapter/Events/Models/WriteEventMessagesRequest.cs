using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using DataCore.Adapter.Common.Models;

namespace DataCore.Adapter.Events.Models {

    /// <summary>
    /// Describes a request to write messages to an event sink.
    /// </summary>
    public class WriteEventMessagesRequest : AdapterRequest {

        /// <summary>
        /// The event messages to write.
        /// </summary>
        [Required]
        [MinLength(1)]
        public EventMessage[] Events { get; set; }


        /// <summary>
        /// Validates the request.
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

            if (Events.Any(x => x == null)) {
                yield return new ValidationResult(Resources.Error_EventMessageCannotBeNull, new[] { nameof(Events) });
            }
        }

    }
}
