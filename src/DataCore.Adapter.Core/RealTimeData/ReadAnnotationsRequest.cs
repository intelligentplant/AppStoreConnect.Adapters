using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataCore.Adapter.RealTimeData {

    /// <summary>
    /// Describes a request to read annotations on tag values.
    /// </summary>
    public sealed class ReadAnnotationsRequest: ReadHistoricalTagValuesRequest { 
    
        /// <summary>
        /// The maximum number of annotations to retrieve per tag.
        /// </summary>
        [Required]
        [DefaultValue(50)]
        public int AnnotationCount { get; set; }

    }

}
