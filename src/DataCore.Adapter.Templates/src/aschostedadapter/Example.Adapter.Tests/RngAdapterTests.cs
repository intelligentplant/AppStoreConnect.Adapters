namespace Example.Adapter.Tests {

    [TestClass]
    public class RngAdapterTests : AdapterTestsBase<RngAdapter> {

        private static IServiceProvider s_serviceProvider = null!;


        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            var services = new ServiceCollection();

            services.AddLogging();

            services.AddDataCoreAdapterServices()
                .AddAdapterOptions<RngAdapterOptions>();

            s_serviceProvider = services.BuildServiceProvider();
        }


        protected override IServiceScope? CreateServiceScope(TestContext context) => s_serviceProvider.CreateScope();


        protected override RngAdapter CreateAdapter(TestContext context, IServiceProvider? serviceProvider) 
            => ActivatorUtilities.CreateInstance<RngAdapter>(serviceProvider!, context.TestName!);

    }

}