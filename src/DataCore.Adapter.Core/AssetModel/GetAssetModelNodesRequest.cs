using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
        [MaxLength(500)]
        public IEnumerable<string> Nodes { get; set; } = Array.Empty<string>();


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

            if (Nodes.Any(x => x == null)) {
                yield return new ValidationResult(SharedResources.Error_NodeIdCannotBeNull, new[] { nameof(Nodes) });
            }

            if (Nodes.Any(x => x?.Length > 200)) {
                yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_IdIsTooLong, 200), new[] { nameof(Nodes) });
            }
        }

    }
}
