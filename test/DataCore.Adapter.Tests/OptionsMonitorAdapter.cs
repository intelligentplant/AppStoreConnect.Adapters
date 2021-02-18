using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

namespace DataCore.Adapter.Tests {
    public class OptionsMonitorAdapter : AdapterBase<OptionsMonitorAdapterOptions> {

        public DateTime UtcOptionsTime => Options.UtcOptionsTime;


        public OptionsMonitorAdapter(string id, IOptions<OptionsMonitorAdapterOptions> options)
            : base(id, options) { }


        public OptionsMonitorAdapter(string id, IOptionsMonitor<OptionsMonitorAdapterOptions> options)
            : base(id, options) { }


        protected override Task StartAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        protected override Task StopAsync(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

    }


    public class OptionsMonitorAdapterOptions : AdapterOptions {

        public DateTime UtcOptionsTime { get; set; } = DateTime.UtcNow;

    }

}
