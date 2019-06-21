using System;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {
    public class SignalRAdapterProxyOptions {

        public string AdapterId { get; set; }

        public Func<string, HubConnection> ConnectionFactory { get; set; }

    }
}
