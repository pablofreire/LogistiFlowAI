import { Card, Group, Pagination, SegmentedControl, Select, Stack, Table, Text, Title } from '@mantine/core';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useShipmentExceptions } from '../api/hooks';
import { shipmentExceptionTypes, type ShipmentExceptionStatus, type ShipmentExceptionType } from '../api/types';
import { formatDateTime, formatNumber } from '../components/format';
import { ResolveExceptionButton } from '../components/ResolveExceptionButton';
import { EmptyState, ErrorState, LoadingState } from '../components/states/QueryState';
import { StatusBadge } from '../components/StatusBadge';

const pageSize = 20;

export function ExceptionsPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<ShipmentExceptionStatus | 'All'>('Open');
  const [type, setType] = useState<ShipmentExceptionType | null>(null);
  const { data, isPending, error } = useShipmentExceptions({ page, pageSize, status: status === 'All' ? null : status, type });

  return (
    <Stack gap="lg">
      <div>
        <Title order={1} fz={26}>
          Logistics exceptions
        </Title>
        <Text c="dimmed" size="sm">
          Delays, failed delivery attempts, returns and carrier outages. Delivery resolves most of them on its own.
        </Text>
      </div>

      <Card>
        <Group mb="md" gap="lg">
          <SegmentedControl
            value={status}
            onChange={(value) => {
              setStatus(value as ShipmentExceptionStatus | 'All');
              setPage(1);
            }}
            data={['Open', 'Resolved', 'All']}
            data-testid="exceptions-status-filter"
          />
          <Select
            placeholder="All types"
            clearable
            data={shipmentExceptionTypes}
            value={type}
            onChange={(value) => {
              setType(value as ShipmentExceptionType | null);
              setPage(1);
            }}
            aria-label="Filter by type"
            data-testid="exceptions-type-filter"
            w={240}
          />
        </Group>

        {isPending ? (
          <LoadingState label="Loading exceptions…" />
        ) : error ? (
          <ErrorState error={error} title="Exceptions unavailable" />
        ) : data.items.length === 0 ? (
          <EmptyState title="Nothing here">No exception matches these filters.</EmptyState>
        ) : (
          <>
            <Table.ScrollContainer minWidth={820}>
              <Table highlightOnHover data-testid="exceptions-table">
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Order</Table.Th>
                    <Table.Th>Type</Table.Th>
                    <Table.Th>Details</Table.Th>
                    <Table.Th>Status</Table.Th>
                    <Table.Th>Opened</Table.Th>
                    <Table.Th />
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {data.items.map((exception) => (
                    <Table.Tr
                      key={exception.id}
                      data-testid="exception-row"
                      data-order-number={exception.orderNumber}
                      data-type={exception.type}
                      data-status={exception.status}
                    >
                      <Table.Td>
                        <Link to={`/shipments/${exception.shipmentId}`}>{exception.orderNumber}</Link>
                        <Text size="xs" c="dimmed" ff="monospace">
                          {exception.trackingNumber ?? exception.carrierCode}
                        </Text>
                      </Table.Td>
                      <Table.Td>
                        <StatusBadge status={exception.type} />
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm">{exception.details}</Text>
                      </Table.Td>
                      <Table.Td>
                        <StatusBadge status={exception.status} />
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" c="dimmed">
                          {formatDateTime(exception.openedAt)}
                        </Text>
                      </Table.Td>
                      <Table.Td>{exception.status === 'Open' && <ResolveExceptionButton exceptionId={exception.id} />}</Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
            <Group justify="space-between" mt="md">
              <Text size="sm" c="dimmed">
                {formatNumber(data.totalCount)} exceptions
              </Text>
              <Pagination total={data.totalPages ?? 1} value={page} onChange={setPage} size="sm" />
            </Group>
          </>
        )}
      </Card>
    </Stack>
  );
}
