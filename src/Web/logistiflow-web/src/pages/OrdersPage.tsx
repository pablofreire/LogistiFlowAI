import { Button, Card, Group, Pagination, Select, Stack, Table, Text, TextInput, Title } from '@mantine/core';
import { useDebouncedValue } from '@mantine/hooks';
import { IconPlus, IconSearch } from '@tabler/icons-react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useOrders } from '../api/hooks';
import { orderStatuses, type OrderStatus } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { formatDateTime, formatNumber } from '../components/format';
import { EmptyState, ErrorState, LoadingState } from '../components/states/QueryState';
import { StatusBadge } from '../components/StatusBadge';

const pageSize = 20;

export function OrdersPage() {
  const { hasRole } = useAuth();
  const navigate = useNavigate();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<OrderStatus | null>(null);
  const [search, setSearch] = useState('');
  const [debouncedSearch] = useDebouncedValue(search, 300);
  const { data, isPending, error, isFetching } = useOrders({ page, pageSize, status, search: debouncedSearch });

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-end">
        <div>
          <Title order={1} fz={26}>
            Orders
          </Title>
          <Text c="dimmed" size="sm">
            Every order received, newest first.
          </Text>
        </div>
        {hasRole('Admin', 'OpsManager') && (
          <Button component={Link} to="/orders/new" leftSection={<IconPlus size={16} />} data-testid="new-order">
            New order
          </Button>
        )}
      </Group>

      <Card>
        <Group mb="md" grow preventGrowOverflow={false} wrap="wrap">
          <TextInput
            placeholder="Search order number or customer"
            leftSection={<IconSearch size={16} />}
            value={search}
            onChange={(event) => {
              setSearch(event.currentTarget.value);
              setPage(1);
            }}
            aria-label="Search orders"
            data-testid="orders-search"
          />
          <Select
            placeholder="All statuses"
            clearable
            data={orderStatuses}
            value={status}
            onChange={(value) => {
              setStatus(value as OrderStatus | null);
              setPage(1);
            }}
            aria-label="Filter by status"
            data-testid="orders-status-filter"
            maw={240}
          />
        </Group>

        {isPending && <LoadingState label="Loading orders…" />}
        {error && <ErrorState error={error} title="Orders unavailable" />}
        {data && data.items.length === 0 && <EmptyState title="No orders match">Adjust the filters or create a new order.</EmptyState>}
        {data && data.items.length > 0 && (
          <>
            <Table.ScrollContainer minWidth={720}>
              <Table highlightOnHover verticalSpacing="sm" data-testid="orders-table" style={{ opacity: isFetching ? 0.7 : 1 }}>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Order</Table.Th>
                    <Table.Th>Customer</Table.Th>
                    <Table.Th>Destination</Table.Th>
                    <Table.Th>Status</Table.Th>
                    <Table.Th ta="right">Units</Table.Th>
                    <Table.Th>Created</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {data.items.map((order) => (
                    <Table.Tr key={order.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`/orders/${order.id}`)} data-testid="order-row" data-order-number={order.orderNumber}>
                      <Table.Td>
                        <Link to={`/orders/${order.id}`} onClick={(event) => event.stopPropagation()}>
                          {order.orderNumber}
                        </Link>
                      </Table.Td>
                      <Table.Td>{order.customerName}</Table.Td>
                      <Table.Td>{order.destinationCity}</Table.Td>
                      <Table.Td>
                        <StatusBadge status={order.status} />
                      </Table.Td>
                      <Table.Td ta="right">{formatNumber(order.totalUnits)}</Table.Td>
                      <Table.Td>
                        <Text size="sm" c="dimmed">
                          {formatDateTime(order.createdAt)}
                        </Text>
                      </Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
            <Group justify="space-between" mt="md">
              <Text size="sm" c="dimmed" data-testid="orders-total">
                {formatNumber(data.totalCount)} orders
              </Text>
              <Pagination total={data.totalPages ?? 1} value={page} onChange={setPage} size="sm" data-testid="orders-pagination" />
            </Group>
          </>
        )}
      </Card>
    </Stack>
  );
}
