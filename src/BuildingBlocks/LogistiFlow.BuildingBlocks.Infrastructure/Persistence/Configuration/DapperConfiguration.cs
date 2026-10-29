using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.TypeHandlers;

namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Configuration;

public static class DapperConfiguration
{
    private static int _configured;

    /// <summary>
    /// Maps snake_case columns to PascalCase properties and registers type handlers. Idempotent and thread-safe.
    /// </summary>
    public static void Configure()
    {
        if (Interlocked.Exchange(ref _configured, 1) == 1)
        {
            return;
        }

        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
    }
}
