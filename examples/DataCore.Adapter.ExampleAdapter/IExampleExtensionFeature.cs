#pragma warning disable CS0618 // Type or member is obsolete
using DataCore.Adapter.Extensions;

namespace DataCore.Adapter.Example {

    /// <summary>
    /// Example adapter extension feature.
    /// </summary>
    [ExtensionFeature(
        "example/ping-pong",
        Name = "Ping Pong",
        Description = "Responds to every ping message with a pong message"
    )]
    public interface IExampleExtensionFeature : IAdapterExtensionFeature {

        [ExtensionFeatureOperation(typeof(ExampleAdapter.ExampleExtensionImpl), nameof(ExampleAdapter.ExampleExtensionImpl.GetPingDescriptor))]
        InvocationResponse Ping(
            IAdapterCallContext context,
            string correlationId
        );

    }

}
#pragma warning restore CS0618 // Type or member is obsolete
