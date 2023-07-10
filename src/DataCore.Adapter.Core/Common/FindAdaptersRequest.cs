using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Globalization;

namespace DataCore.Adapter.Common {

    /// <summary>
    /// A request to retrieve a filtered list of adapters.
    /// </summary>
    public class FindAdaptersRequest : AdapterRequest, IPageableAdapterRequest {

        /// <summary>
        /// The adapter ID filter. Partial matches can be specified.
        /// </summary>
        /// <remarks>
        ///   Partial matches can be specified; <c>?</c> and <c>*</c> can be used as single- and 
        ///   multi-character wildcards respectively.
        /// </remarks>
        [MaxLength(AdapterDescriptor.IdMaxLength)]
        public string? Id { get; set; }

        /// <summary>
        /// The adapter name filter.
        /// </summary>
        /// <remarks>
        ///   Partial matches can be specified; <c>?</c> and <c>*</c> can be used as single- and 
        ///   multi-character wildcards respectively.
        /// </remarks>
        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>
        /// The adapter description filter.
        /// </summary>
        /// <remarks>
        ///   Partial matches can be specified; <c>?</c> and <c>*</c> can be used as single- and 
        ///   multi-character wildcards respectively.
        /// </remarks>
        [MaxLength(200)]
        public string? Description { get; set; }

        /// <summary>
        /// The adapter feature filters.
        /// </summary>
        /// <remarks>
        ///   Unlike the <see cref="Id"/>, <see cref="Name"/>, and <see cref="Description"/> 
        ///   filters, the <see cref="Features"/> filters must exactly match the name of a 
        ///   standard or extension feature.
        /// </remarks>
        [MaxLength(10)]
        public IEnumerable<string>? Features { get; set; }

        /// <inheritdoc/>
        [Range(1, 500)]
        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;

        /// <inheritdoc/>
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int Page { get; set; } = 1;


        /// <inheritdoc/>
        protected override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            foreach (var item in base.Validate(validationContext)) {
                yield return item;
            }

            if (Features != null) {
                if (Features.Any(x => x != null && x.Length > 200)) {
                    yield return new ValidationResult(string.Format(CultureInfo.CurrentCulture, SharedResources.Error_CollectionItemIsTooLong, 200), new[] { nameof(Features) });
                }
            }
        }

    }
}
