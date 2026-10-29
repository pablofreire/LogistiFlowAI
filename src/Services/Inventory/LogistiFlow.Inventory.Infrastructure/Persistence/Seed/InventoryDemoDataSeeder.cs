using Dapper;
using LogistiFlow.BuildingBlocks.Infrastructure.Persistence.Interfaces;

namespace LogistiFlow.Inventory.Infrastructure.Persistence.Seed;

/// <summary>
/// Deterministic catalog for local runs, end-to-end tests and load tests. Idempotent: existing rows are kept,
/// so stock consumed by earlier runs is not reset. Some SKUs exist for specific scenarios:
/// <list type="bullet">
/// <item><c>SKU-OOS-001</c>: no stock anywhere (insufficient-inventory path).</item>
/// <item><c>SKU-LIMITED-001</c>: 10 units in one warehouse (contention demo).</item>
/// <item><c>SKU-BULK-001</c>: one million units (k6 load tests).</item>
/// </list>
/// </summary>
public sealed class InventoryDemoDataSeeder(IDbConnectionFactory connectionFactory) : IDemoDataSeeder
{
    private const string Sql = """
        INSERT INTO inventory.warehouses (public_id, code, name, city, country, is_active, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'WH-NJ1', 'Newark Fulfillment Center', 'Newark', 'US', true, now(), now()),
            (gen_random_uuid(), 'WH-TX1', 'Dallas Distribution Center', 'Dallas', 'US', true, now(), now()),
            (gen_random_uuid(), 'WH-CA1', 'Ontario Fulfillment Center', 'Ontario', 'US', true, now(), now())
        ON CONFLICT (code) DO NOTHING;

        INSERT INTO inventory.products (public_id, sku, name, description, is_active, created_at, updated_at)
        VALUES
            (gen_random_uuid(), 'SKU-1001', 'Wireless Barcode Scanner', '2D imager, Bluetooth, 2,400 mAh battery', true, now(), now()),
            (gen_random_uuid(), 'SKU-1002', 'Thermal Label Printer', '4-inch direct thermal, 203 dpi', true, now(), now()),
            (gen_random_uuid(), 'SKU-1003', 'Shipping Label Roll 4x6', '500 labels per roll', true, now(), now()),
            (gen_random_uuid(), 'SKU-1004', 'Corrugated Box 12x12x12', 'Pack of 25, 32 ECT', true, now(), now()),
            (gen_random_uuid(), 'SKU-1005', 'Packing Tape Dispenser', 'Pistol grip, 2-inch tape', true, now(), now()),
            (gen_random_uuid(), 'SKU-1006', 'Handheld Mobile Computer', 'Android rugged terminal with scanner', true, now(), now()),
            (gen_random_uuid(), 'SKU-1007', 'Manual Pallet Jack', '5,500 lb capacity', true, now(), now()),
            (gen_random_uuid(), 'SKU-1008', 'Stretch Wrap Film 18 in', '1,500 ft, 80 gauge', true, now(), now()),
            (gen_random_uuid(), 'SKU-LOW-001', 'Barcode Ribbon Cartridge', 'Wax-resin ribbon, low-stock example', true, now(), now()),
            (gen_random_uuid(), 'SKU-LIMITED-001', 'Smart Parcel Scale', 'Limited allocation: 10 units', true, now(), now()),
            (gen_random_uuid(), 'SKU-OOS-001', 'RFID Portal Reader', 'Out of stock in every warehouse', true, now(), now()),
            (gen_random_uuid(), 'SKU-BULK-001', 'Poly Mailer 10x13', 'Load-test SKU with one million units', true, now(), now())
        ON CONFLICT (sku) DO NOTHING;

        INSERT INTO inventory.stock_positions (public_id, product_id, warehouse_id, on_hand, reserved, is_active, created_at, updated_at)
        SELECT gen_random_uuid(), p.id, w.id, s.on_hand, 0, true, now(), now()
        FROM (VALUES
            ('SKU-1001', 'WH-NJ1', 500), ('SKU-1001', 'WH-TX1', 300), ('SKU-1001', 'WH-CA1', 200),
            ('SKU-1002', 'WH-NJ1', 120), ('SKU-1002', 'WH-TX1', 80),  ('SKU-1002', 'WH-CA1', 60),
            ('SKU-1003', 'WH-NJ1', 2000), ('SKU-1003', 'WH-TX1', 1500), ('SKU-1003', 'WH-CA1', 1500),
            ('SKU-1004', 'WH-NJ1', 800), ('SKU-1004', 'WH-TX1', 600), ('SKU-1004', 'WH-CA1', 400),
            ('SKU-1005', 'WH-NJ1', 300), ('SKU-1005', 'WH-TX1', 150), ('SKU-1005', 'WH-CA1', 150),
            ('SKU-1006', 'WH-NJ1', 60),  ('SKU-1006', 'WH-TX1', 40),
            ('SKU-1007', 'WH-NJ1', 8),   ('SKU-1007', 'WH-TX1', 5),   ('SKU-1007', 'WH-CA1', 3),
            ('SKU-1008', 'WH-NJ1', 400), ('SKU-1008', 'WH-TX1', 250), ('SKU-1008', 'WH-CA1', 250),
            ('SKU-LOW-001', 'WH-NJ1', 6), ('SKU-LOW-001', 'WH-TX1', 4),
            ('SKU-LIMITED-001', 'WH-NJ1', 10),
            ('SKU-OOS-001', 'WH-NJ1', 0), ('SKU-OOS-001', 'WH-TX1', 0),
            ('SKU-BULK-001', 'WH-NJ1', 1000000)
        ) AS s(sku, warehouse_code, on_hand)
        JOIN inventory.products p ON p.sku = s.sku
        JOIN inventory.warehouses w ON w.code = s.warehouse_code
        ON CONFLICT (product_id, warehouse_id) DO NOTHING;
        """;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(Sql, cancellationToken: cancellationToken));
    }
}
