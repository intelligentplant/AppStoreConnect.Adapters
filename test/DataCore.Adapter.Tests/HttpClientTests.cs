#if NETCOREAPP
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using DataCore.Adapter.Http.Client;
using DataCore.Adapter.RealTimeData;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class HttpClientTests : TestsBase {

        [TestMethod]
        public async Task HttpClientShouldDeserializeProblemDetailsResponse() {
            var client = AssemblyInitializer.ApplicationServices.GetRequiredService<AdapterHttpClient>();
            client.DefaultRequestVersion = new Version(2, 0);

            // Request will fail validation because it does not specify any tags.
            var requestContent = new ReadSnapshotTagValuesRequest();
            var url = "api/app-store-connect/v2.0/tag-values/" + WebHostConfiguration.AdapterId + "/snapshot";

            using (var ctSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            using (var request = client.CreateHttpRequestMessage(HttpMethod.Post, url, requestContent, null))
            using (var response = await client.HttpClient.SendAsync(request, ctSource.Token).ConfigureAwait(false)) {
                Assert.IsFalse(response.IsSuccessStatusCode);
                try {
                    await response.ThrowOnErrorResponse().ConfigureAwait(false);
                    Assert.Fail("Error should have been thrown.");
                }
                catch (AdapterHttpClientException e) {
                    Assert.IsNotNull(e.ProblemDetails);
                    // The type for the problem details is only set in ASP.NET Core 3.x onwards.
                    Assert.IsNotNull(e.ProblemDetails.Type);
                    Assert.IsNotNull(e.ProblemDetails.Title);
                    Assert.IsNotNull(e.ProblemDetails.Extensions);
                }
            }
        }

    }

}
#endif
