import { Card, Checkbox, Group, Pagination, Select, SimpleGrid, Stack, Table, Text, TextInput, Title } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { useStock, useWarehouses } from '../api/hooks';
import { formatNumber } from '../components/format';
import { EmptyState, ErrorState, LoadingState } from '../components/states/QueryState';

export function InventoryPage() {
  const [page, setPage] = useState(1);
  const [warehouseCode, setWarehouseCode] = useState<string | null>(null);
  const [sku, setSku] = useState('');
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [debouncedSku] = useDebouncedValue(sku, 300);
  const warehouses = useWarehouses();
  const stock = useStock({ page, pageSize: 25, warehouseCode, sku: debouncedSku, lowStockOnly });

  return (
    <Stack gap="lg">
      <div>
        <Title order={1} fz={26}>
          Inventory
        </Title>
        <Text c="dimmed" size="sm">
          Available = on hand − reserved. Reservations are taken atomically per stock position.
        </Text>
      </div>

      <SimpleGrid cols={{ base: 1, sm: 3 }} data-testid="warehouse-cards">
        {warehouses.data?.map((warehouse) => (
          <Card key={warehouse.code} padding="md">
            <Text fw={700}>{warehouse.code}</Text>
            <Text size="sm" c="dimmed">
              {warehouse.name} · {warehouse.city}
            </Text>
            <Group mt="sm" gap="lg">
              <Text size="sm">
                On hand <b>{formatNumber(warehouse.onHand)}</b>
              </Text>
              <Text size="sm">
                Reserved <b>{formatNumber(warehouse.reserved)}</b>
              </Text>
            </Group>
          </Card>
        ))}
      </SimpleGrid>

      <Card>
        <Group mb="md" wrap="wrap">
          <TextInput
            placeholder="SKU"
            leftSection={<IconSearch size={16} />}
            value={sku}
            onChange={(event) => {
              setSku(event.currentTarget.value);
              setPage(1);
            }}
            aria-label="Filter by SKU"
            data-testid="stock-sku-filter"
          />
          <Select
            placeholder="All warehouses"
            clearable
            data={warehouses.data?.map((warehouse) => warehouse.code) ?? []}
            value={warehouseCode}
            onChange={(value) => {
              setWarehouseCode(value);
              setPage(1);
            }}
            aria-label="Filter by warehouse"
            data-testid="stock-warehouse-filter"
          />
          <Checkbox
            label="Low stock only"
            checked={lowStockOnly}
            onChange={(event) => {
              setLowStockOnly(event.currentTarget.checked);
              setPage(1);
            }}
            data-testid="stock-low-only"
          />
        </Group>

        {stock.isPending && <LoadingState label="Loading stock…" />}
        {stock.error && <ErrorState error={stock.error} title="Stock unavailable" />}
        {stock.data && stock.data.items.length === 0 && <EmptyState title="No stock positions match" />}
        {stock.data && stock.data.items.length > 0 && (
          <>
            <Table.ScrollContainer minWidth={640}>
              <Table striped highlightOnHover data-testid="stock-table">
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>SKU</Table.Th>
                    <Table.Th>Product</Table.Th>
                    <Table.Th>Warehouse</Table.Th>
                    <Table.Th ta="right">On hand</Table.Th>
                    <Table.Th ta="right">Reserved</Table.Th>
                    <Table.Th ta="right">Available</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {stock.data.items.map((position) => (
                    <Table.Tr key={position.id} data-testid="stock-row" data-sku={position.sku} data-warehouse={position.warehouseCode}>
                      <Table.Td>{position.sku}</Table.Td>
                      <Table.Td>{position.productName}</Table.Td>
                      <Table.Td>{position.warehouseCode}</Table.Td>
                      <Table.Td ta="right">{formatNumber(position.onHand)}</Table.Td>
                      <Table.Td ta="right" data-testid="stock-reserved">
                        {formatNumber(position.reserved)}
                      </Table.Td>
                      <Table.Td ta="right" fw={600} c={position.available <= 10 ? 'red' : undefined} data-testid="stock-available">
                        {formatNumber(position.available)}
                      </Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
            <Group justify="flex-end" mt="md">
              <Pagination total={stock.data.totalPages ?? 1} value={page} onChange={setPage} size="sm" />
            </Group>
          </>
        )}
      </Card>
    </Stack>
  );
}
