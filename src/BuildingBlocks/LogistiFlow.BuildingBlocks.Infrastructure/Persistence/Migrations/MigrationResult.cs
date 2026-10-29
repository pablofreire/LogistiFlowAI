namespace LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Migrations;

public sealed record MigrationResult(bool Successful, IReadOnlyList<string> AppliedScripts, string? ErrorMessage);
