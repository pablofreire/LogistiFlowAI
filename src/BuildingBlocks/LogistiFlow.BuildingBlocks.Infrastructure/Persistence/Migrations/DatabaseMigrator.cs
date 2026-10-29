using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Extensions.Logging;

namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Migrations;

/// <summary>
/// Applies the SQL migrations embedded in the given assemblies to one service database.
/// Scripts are ordered by file name (not by assembly), so the numbering scheme
/// <c>{NNNN}_{schema}_{entity}_{action}.sql</c> defines a single order: 00xx shared messaging tables, then the service's own range.
/// </summary>
public sealed partial class DatabaseMigrator(ILogger<DatabaseMigrator> logger)
{
    public MigrationResult Migrate(string connectionString, IEnumerable<Assembly> moduleAssemblies)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var scripts = LoadScripts(moduleAssemblies);
        LogDiscovered(logger, scripts.Count);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScripts(scripts)
            .WithTransactionPerScript()
            .JournalToPostgresqlTable("public", "schema_versions")
            .LogTo(new DbUpLoggerAdapter(logger))
            .Build();

        var result = upgrader.PerformUpgrade();
        var applied = result.Scripts.Select(script => script.Name).ToArray();
        if (!result.Successful)
        {
            LogFailed(logger, result.Error, result.ErrorScript?.Name);
            return new MigrationResult(false, applied, result.Error?.Message);
        }

        LogCompleted(logger, applied.Length);
        return new MigrationResult(true, applied, null);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Discovered {ScriptCount} migration scripts")]
    private static partial void LogDiscovered(ILogger logger, int scriptCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Migration failed on script {Script}")]
    private static partial void LogFailed(ILogger logger, Exception? exception, string? script);

    [LoggerMessage(Level = LogLevel.Information, Message = "Database is up to date ({AppliedCount} scripts applied)")]
    private static partial void LogCompleted(ILogger logger, int appliedCount);

    private static List<SqlScript> LoadScripts(IEnumerable<Assembly> assemblies)
    {
        var scripts = new List<SqlScript>();
        foreach (var assembly in assemblies)
        {
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));

            foreach (var resourceName in resourceNames)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' could not be opened.");
                using var reader = new StreamReader(stream);
                scripts.Add(new SqlScript(ToFileName(resourceName), reader.ReadToEnd()));
            }
        }

        var duplicates = scripts
            .GroupBy(script => script.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate migration script names: {string.Join(", ", duplicates)}");
        }

        return scripts.OrderBy(script => script.Name, StringComparer.Ordinal).ToList();
    }

    // "LogistiFlow.Orders.Infrastructure.Persistence.Migrations.0101_orders_schema_create.sql" -> "0101_orders_schema_create.sql"
    private static string ToFileName(string resourceName)
    {
        var withoutExtension = resourceName[..^".sql".Length];
        var lastDot = withoutExtension.LastIndexOf('.');
        return string.Concat(withoutExtension.AsSpan(lastDot + 1), ".sql");
    }
}
