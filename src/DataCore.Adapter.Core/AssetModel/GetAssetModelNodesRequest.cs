using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DataCore.Adapter.Common;

namespace DataCore.Adapter.AssetModel {
    /// <summary>
    /// Describes a request to get asset model nodes by ID.
    /// </summary>
    public sealed class GetAssetModelNodesRequest : AdapterRequest {

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
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if (Nodes.Any(x => x == null)) {
                yield return new ValidationResult(SharedResources.Error_NodeIdCannotBeNull, new[] { nameof(Nodes) });
            }
        }

    }
}
