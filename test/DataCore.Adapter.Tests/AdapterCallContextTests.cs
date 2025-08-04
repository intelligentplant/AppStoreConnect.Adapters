using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataCore.Adapter.Tests {

    [TestClass]
    public class AdapterCallContextTests : TestsBase {

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            TypeExtensions.AddStandardFeatureDefinition(typeof(ITestFeature));
        }


        [TestMethod]
        [DataRow(null, false)]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ", false)]
        [DataRow("Hello", true)]
        public async Task RequestShouldSkipValidation(string invalidName, bool isValid) {
            using var adapter = new ExampleAdapter();
            adapter.AddFeature<ITestFeature>(new TestFeatureWrapper(adapter, new TestFeature()));

            await ((IAdapter) adapter).StartAsync(default);

            var context = ExampleCallContext.ForPrincipal(ClaimsPrincipal.Current);
            var feature = adapter.GetFeature<ITestFeature>();

            var request = new TestFeatureRequest() { 
                Name = invalidName
            };

            context.UseRequestValidation(false);

            Assert.AreEqual(isValid, await feature.IsValid(context, request, default));
        }


        [TestMethod]
        [DataRow(null, false)]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ", false)]
        [DataRow("Hello", true)]
        public async Task RequestShouldNotSkipValidation(string name, bool isValid) {
            using var adapter = new ExampleAdapter();
            adapter.AddFeature<ITestFeature>(new TestFeatureWrapper(adapter, new TestFeature()));

            await ((IAdapter) adapter).StartAsync(default);

            var context = ExampleCallContext.ForPrincipal(ClaimsPrincipal.Current);
            var feature = adapter.GetFeature<ITestFeature>();

            var request = new TestFeatureRequest() {
                Name = name
            };

            if (isValid) {
                Assert.IsTrue(await feature.IsValid(context, request, default).ConfigureAwait(false));
            }
            else {
                await Assert.ThrowsExactlyAsync<ValidationException>(() => feature.IsValid(context, request, default)).ConfigureAwait(false);
            }
        }


        [AdapterFeature("unit-tests:test-feature")]
        private interface ITestFeature : IAdapterFeature {

            Task<bool> IsValid(IAdapterCallContext context, TestFeatureRequest request, CancellationToken cancellationToken);
        
        }


        private class TestFeatureRequest {

            [Required]
            [MaxLength(10)]
            public string Name { get; set; }

        }


        private class TestFeature : ITestFeature {

            public Task<bool> IsValid(IAdapterCallContext context, TestFeatureRequest request, CancellationToken cancellationToken) {
                if (request == null) {
                    return Task.FromResult(false);
                }

                var validationResults = new List<ValidationResult>();
                return Task.FromResult(Validator.TryValidateObject(request, new ValidationContext(request), validationResults, true));
            }

        }


        private class TestFeatureWrapper : AdapterFeatureWrapper<ITestFeature>, ITestFeature { 
        
            public TestFeatureWrapper(ExampleAdapter adapter, ITestFeature feature) : base(adapter, feature) { }

            public Task<bool> IsValid(IAdapterCallContext context, TestFeatureRequest request, CancellationToken cancellationToken) {
                return InvokeAsync(context, request, InnerFeature.IsValid, cancellationToken);
            }
        }

    }

}
