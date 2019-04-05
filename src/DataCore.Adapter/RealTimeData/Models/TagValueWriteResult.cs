using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace DataCore.Adapter.RealTimeData.Models {
    public sealed class TagValueWriteResult {

        public string TagId { get; }

        public bool Success { get; }

        public string Notes { get; }

        public IDictionary<string, string> Properties { get; }


        public TagValueWriteResult(string tagId, bool success, string notes, IDictionary<string, string> properties) {
            TagId = tagId ?? throw new ArgumentNullException(nameof(tagId));
            Success = success;
            Notes = notes?.Trim();
            Properties = new ReadOnlyDictionary<string, string>(properties ?? new Dictionary<string, string>());
        }

    }
}
