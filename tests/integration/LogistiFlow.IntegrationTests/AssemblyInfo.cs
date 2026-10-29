using LogistiFlow.IntegrationTests.Fixtures;

// One PostgreSQL and one RabbitMQ container for the whole run; tests share them and run sequentially.
[assembly: AssemblyFixture(typeof(LogistiFlowEnvironment))]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
