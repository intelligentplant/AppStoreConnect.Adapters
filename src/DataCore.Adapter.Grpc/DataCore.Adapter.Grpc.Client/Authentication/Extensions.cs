using System.Collections.Generic;

using GrpcCore = Grpc.Core;

namespace DataCore.Adapter.Grpc.Client.Authentication {

    /// <summary>
    /// Extensions for <see cref="IClientCallCredentials"/>.
    /// </summary>
    public static class Extensions {

        /// <summary>
        /// Adds a metadata entry to the specified <see cref="GrpcCore.Metadata"/> collection.
        /// </summary>
        /// <param name="credentials">
        ///   The credentials.
        /// </param>
        /// <param name="metadata">
        ///   The gRPC metadata collection to add the entry to.
        /// </param>
        public static void AddMetadataEntry(this IClientCallCredentials credentials, GrpcCore.Metadata metadata) {
            if (credentials == null || metadata == null) {
                return;
            }

            var entry = credentials.GetMetadataEntry();
            if (entry == null) {
                return;
            }

            metadata.Add(entry);
        }


        /// <summary>
        /// Adds metadata entries to the specified <see cref="GrpcCore.Metadata"/> collection.
        /// </summary>
        /// <param name="credentials">
        ///   The credentials.
        /// </param>
        /// <param name="metadata">
        ///   The gRPC metadata collection to add the entry to.
        /// </param>
        public static void AddMetadataEntries(this IEnumerable<IClientCallCredentials> credentials, GrpcCore.Metadata metadata) {
            if (credentials == null || metadata == null) {
                return;
            }

            foreach (var item in credentials) {
                item.AddMetadataEntry(metadata);
            }
        }

    }
}
