namespace LogistiFlow.Inventory.Infrastructure.Persistence.Queries;

internal static class ProductQueries
{
    public const string Insert = """
        INSERT INTO inventory.products (public_id, sku, name, description, is_active, created_at, updated_at)
        VALUES (@PublicId, @Sku, @Name, @Description, true, @CreatedAt, @UpdatedAt)
        ON CONFLICT (sku) DO NOTHING
        RETURNING id
        """;
}
