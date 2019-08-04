# DataCore.Adapter.HttpClient

Client for querying remote adapters via HTTP.

This client supports polling requests only; as such, it cannot be used to subscribe to receive snapshot tag value changes, or event messages published by remote adapters. For push-related functionality, use the gRPC or SignalR client.