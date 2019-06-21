using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataCore.Adapter.AspNetCore.SignalR.Proxy {
    public static class ProxyAdapterFeatureExtensions {

        public static Task<HubConnection> GetAssetModelBrowserHubConnection(this ProxyAdapterFeature feature, CancellationToken cancellationToken = default) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return feature.GetHubConnection($"{SignalRAdapterProxy.HubRoutePrefix}/asset-model-browser", cancellationToken);
        }


        public static Task<HubConnection> GetEventsHubConnection(this ProxyAdapterFeature feature, CancellationToken cancellationToken = default) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return feature.GetHubConnection($"{SignalRAdapterProxy.HubRoutePrefix}/events", cancellationToken);
        }


        public static Task<HubConnection> GetInfoHubConnection(this ProxyAdapterFeature feature, CancellationToken cancellationToken = default) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return feature.GetHubConnection($"{SignalRAdapterProxy.HubRoutePrefix}/info", cancellationToken);
        }


        public static Task<HubConnection> GetTagAnnotationsHubConnection(this ProxyAdapterFeature feature, CancellationToken cancellationToken = default) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return feature.GetHubConnection($"{SignalRAdapterProxy.HubRoutePrefix}/tag-annotations", cancellationToken);
        }


        public static Task<HubConnection> GetTagSearchHubConnection(this ProxyAdapterFeature feature, CancellationToken cancellationToken = default) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return feature.GetHubConnection($"{SignalRAdapterProxy.HubRoutePrefix}/tag-search", cancellationToken);
        }


        public static Task<HubConnection> GetTagValuesHubConnection(this ProxyAdapterFeature feature, CancellationToken cancellationToken = default) {
            if (feature == null) {
                throw new ArgumentNullException(nameof(feature));
            }

            return feature.GetHubConnection($"{SignalRAdapterProxy.HubRoutePrefix}/tag-values", cancellationToken);
        }

    }
}
