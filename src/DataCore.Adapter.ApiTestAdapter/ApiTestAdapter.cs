using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataCore.Adapter.ApiTestAdapter {
    public class ApiTestAdapter : Csv.CsvAdapter {

        public ApiTestAdapter(IOptions<ApiTestAdapterOptions> options, ILoggerFactory loggerFactory)
            : base(options, loggerFactory) {
            
            // TODO: enable/disable features as required?
        }

    }
}
