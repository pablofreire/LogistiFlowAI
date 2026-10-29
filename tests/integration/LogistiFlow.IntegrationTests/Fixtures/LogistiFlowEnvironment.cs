using System.Reflection;
using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Configuration;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Connections;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Migrations;
using LogistiFlow.BuildingBlocks.Messaging.DependencyInjection;
using LogistiFlow.CarrierSimulator.Composition;
using LogistiFlow.Fulfillment.Api.Composition;
using LogistiFlow.Fulfillment.Infrastructure.DependencyInjection;
using LogistiFlow.Inventory.Api.Composition;
using LogistiFlow.Inventory.Infrastructure.DependencyInjection;
using LogistiFlow.Inventory.Infrastructure.Persistence.Seed;
using LogistiFlow.Orders.Api.Composition;
using LogistiFlow.Orders.Infrastructure.DependencyInjection;
using LogistiFlow.Shipping.Api.Composition;
using LogistiFlow.Shipping.Application.Interfaces;
using LogistiFlow.Shipping.Infrastructure.Carriers;
using LogistiFlow.Shipping.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace LogistiFlow.IntegrationTests.Fixtures;

/// <summary>
/// The whole platform for one test run: real PostgreSQL 17 and RabbitMQ 4.1 containers, one database per
/// service, and the four services hosted in-process with their outbox relays and consumers running. Shipping talks
/// to the carrier simulator over its in-memory test server, with compressed time (one tracking step per 200 ms).
/// <para>
/// <see cref="IsolatedOrders"/> is a second Orders instance on its own database with the relay and consumer
/// switched off and the broker address pointing at a closed port. Tests use it to drive messaging by hand.
/// </para>
/// </summary>
public sealed class LogistiFlowEnvironment : IAsyncLifetime
{
    public const string RabbitUser = "logistiflow";
    public const string RabbitPassword = "integration-tests";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithUsername("logistiflow")
        .WithPassword("integration-tests")
        .WithCommand("-c", "max_connections=400")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4.1-management-alpine")
        .WithUsername(RabbitUser)
        .WithPassword(RabbitPassword)
        .Build();

    public ServiceFactory<OrdersApiMarker> Orders { get; private set; } = null!;

    public ServiceFactory<InventoryApiMarker> Inventory { get; private set; } = null!;

    public ServiceFactory<FulfillmentApiMarker> Fulfillment { get; private set; } = null!;

    public ServiceFactory<ShippingApiMarker> Shipping { get; private set; } = null!;

    public ServiceFactory<CarrierSimulatorMarker> CarrierSimulator { get; private set; } = null!;

    public ServiceFactory<OrdersApiMarker> IsolatedOrders { get; private set; } = null!;

    /// <summary>Inventory on its own seeded database with messaging switched off; tests call its use cases directly.</summary>
    public ServiceFactory<InventoryApiMarker> IsolatedInventory { get; private set; } = null!;

    public string RabbitHost => _rabbitMq.Hostname;

    public int RabbitPort => _rabbitMq.GetMappedPublicPort(5672);

    public string ConnectionString(string database) =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = database }.ConnectionString;

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());
        DapperConfiguration.Configure();

        Migrate(Databases.Orders, OrdersInfrastructureServiceCollectionExtensions.MigrationsAssembly);
        Migrate(Databases.Inventory, InventoryInfrastructureServiceCollectionExtensions.MigrationsAssembly);
        Migrate(Databases.Fulfillment, FulfillmentInfrastructureServiceCollectionExtensions.MigrationsAssembly);
        Migrate(Databases.Shipping, ShippingInfrastructureServiceCollectionExtensions.MigrationsAssembly);
        Migrate(Databases.IsolatedOrders, OrdersInfrastructureServiceCollectionExtensions.MigrationsAssembly);
        Migrate(Databases.IsolatedInventory, InventoryInfrastructureServiceCollectionExtensions.MigrationsAssembly);
        Migrate(Databases.Probe);

        foreach (var database in new[] { Databases.Inventory, Databases.IsolatedInventory })
        {
            await using var dataSource = NpgsqlDataSource.Create(ConnectionString(database));
            await new InventoryDemoDataSeeder(new NpgsqlConnectionFactory(dataSource)).SeedAsync(CancellationToken.None);
        }

        Orders = new ServiceFactory<OrdersApiMarker>(Settings(Databases.Orders));
        Inventory = new ServiceFactory<InventoryApiMarker>(Settings(Databases.Inventory));
        Fulfillment = new ServiceFactory<FulfillmentApiMarker>(Settings(Databases.Fulfillment));
        CarrierSimulator = new ServiceFactory<CarrierSimulatorMarker>(new Dictionary<string, string?>
        {
            ["Simulator:StepInterval"] = "00:00:00.200",
            ["Simulator:TimeoutDelay"] = "00:00:03",
            ["Simulator:TemporaryFailureCount"] = "2",
        });
        var shippingSettings = Settings(Databases.Shipping);
        shippingSettings["CarrierApi:BaseAddress"] = "http://carrier-simulator/";
        shippingSettings["CarrierApi:Timeout"] = "00:00:00.750";
        shippingSettings["ShippingWorkers:IdleDelay"] = "00:00:00.100";
        shippingSettings["ShippingWorkers:BookingBaseDelay"] = "00:00:00.200";
        shippingSettings["ShippingWorkers:TrackingPollInterval"] = "00:00:00.200";
        Shipping = new ServiceFactory<ShippingApiMarker>(
            shippingSettings,
            services => services.AddHttpClient<ICarrierGateway, CarrierApiGateway>()
                .ConfigurePrimaryHttpMessageHandler(() => CarrierSimulator.Server.CreateHandler()));
        IsolatedOrders = new ServiceFactory<OrdersApiMarker>(Settings(Databases.IsolatedOrders, brokerPort: 1, messagingEnabled: false));
        IsolatedInventory = new ServiceFactory<InventoryApiMarker>(Settings(Databases.IsolatedInventory, brokerPort: 1, messagingEnabled: false));

        // Starting the hosts starts their relays and consumers, which declare the queues before any test publishes.
        _ = Orders.Server;
        _ = Inventory.Server;
        _ = Fulfillment.Server;
        _ = CarrierSimulator.Server;
        _ = Shipping.Server;
        _ = IsolatedOrders.Server;
        _ = IsolatedInventory.Server;
        await WaitForQueuesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Orders.DisposeAsync();
        await Inventory.DisposeAsync();
        await Fulfillment.DisposeAsync();
        await Shipping.DisposeAsync();
        await CarrierSimulator.DisposeAsync();
        await IsolatedOrders.DisposeAsync();
        await IsolatedInventory.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task<NpgsqlConnection> OpenAsync(string database)
    {
        var connection = new NpgsqlConnection(ConnectionString(database));
        await connection.OpenAsync();
        return connection;
    }

    public Dictionary<string, string?> Settings(string database, int? brokerPort = null, bool messagingEnabled = true) => new()
    {
        ["Database:ConnectionString"] = ConnectionString(database),
        ["RabbitMq:HostName"] = RabbitHost,
        ["RabbitMq:Port"] = (brokerPort ?? RabbitPort).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["RabbitMq:UserName"] = RabbitUser,
        ["RabbitMq:Password"] = RabbitPassword,
        ["Jwt:SigningKey"] = TestTokens.SigningKey,
        ["Messaging:Outbox:RelayEnabled"] = messagingEnabled ? "true" : "false",
        ["Messaging:Outbox:PollingInterval"] = "00:00:00.100",
        ["Messaging:Consumer:Enabled"] = messagingEnabled ? "true" : "false",
        ["Messaging:Consumer:RetryDelays:0"] = "00:00:00.200",
        ["Messaging:Consumer:RetryDelays:1"] = "00:00:00.500",
        ["Messaging:Consumer:RetryDelays:2"] = "00:00:01",
        ["Serilog:MinimumLevel:Default"] = "Warning",
    };

    private void Migrate(string database, params Assembly[] serviceAssemblies)
    {
        var assemblies = new[] { MessagingServiceCollectionExtensions.MigrationsAssembly }.Concat(serviceAssemblies);
        var result = new DatabaseMigrator(NullLogger<DatabaseMigrator>.Instance).Migrate(ConnectionString(database), assemblies);
        if (!result.Successful)
        {
            throw new InvalidOperationException($"Migrating {database} failed: {result.ErrorMessage}");
        }
    }

    private async Task WaitForQueuesAsync()
    {
        var expected = new[] { "orders.integration-events", "inventory.integration-events", "fulfillment.integration-events", "shipping.integration-events" };
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var listing = await _rabbitMq.ExecAsync(["rabbitmqctl", "-q", "list_queues", "name"]);
            if (expected.All(queue => listing.Stdout.Contains(queue, StringComparison.Ordinal)))
            {
                return;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException("The service queues were not declared in time.");
    }

    public static class Databases
    {
        public const string Orders = "logistiflow_orders";
        public const string Inventory = "logistiflow_inventory";
        public const string Fulfillment = "logistiflow_fulfillment";
        public const string Shipping = "logistiflow_shipping";
        public const string IsolatedOrders = "logistiflow_orders_isolated";
        public const string IsolatedInventory = "logistiflow_inventory_isolated";
        public const string Probe = "logistiflow_probe";
    }

    /// <summary>Polls until <paramref name="condition"/> holds; fails with <paramref name="description"/> after the timeout.</summary>
    public static async Task<T> EventuallyAsync<T>(Func<Task<T>> probe, Func<T, bool> condition, string description, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        T value;
        do
        {
            value = await probe();
            if (condition(value))
            {
                return value;
            }

            await Task.Delay(100);
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException($"Timed out waiting for {description}. Last value: {value}");
    }

    public static Task<long> CountAsync(NpgsqlConnection connection, string sql, object? parameters = null) =>
        connection.ExecuteScalarAsync<long>(sql, parameters);
}
