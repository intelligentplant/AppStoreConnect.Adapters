using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using DataCore.Adapter;
using DataCore.Adapter.Extensions;

using IntelligentPlant.BackgroundTasks;

namespace MyAdapter {

    [ExtensionFeature(
        ExtensionUri,
        Name = "Ping Pong",
        Description = "Example extension feature."
    )]
    public class PingPongExtension : AdapterExtensionFeature {

        public const string ExtensionUri = "tutorial/ping-pong/";

        public PingPongExtension(IBackgroundTaskService backgroundTaskService) 
            : base(backgroundTaskService) { }

    }
}
