using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace DataCore.Adapter.AssetModel.Models {
    /// <summary>
    /// Describes a request to get asset model nodes by ID.
    /// </summary>
    public sealed class GetAssetModelNodesRequest : IValidatableObject {

        /// <summary>
        /// The IDs of the nodes to retrieve.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string[] Nodes { get; set; }


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">
        ///   The validation context.
        /// </param>
        /// <returns>
        ///   A collection of validation errors.
        /// </returns>
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext) {
            return Validate(validationContext);
        }


        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="validationContext">
        ///   The validation context.
        /// </param>
        /// <returns>
        ///   A collection of validation errors.
        /// </returns>
        private IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Nodes.Any(x => x == null)) {
                yield return new ValidationResult(SharedResources.Error_NodeIdCannotBeNull, new[] { nameof(Nodes) });
            }
        }

    }
}
