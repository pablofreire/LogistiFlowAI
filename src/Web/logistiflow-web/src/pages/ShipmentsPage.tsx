import { Badge, Card, Group, Pagination, Select, Stack, Switch, Table, Text, Title } from '@mantine/core';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useShipments } from '../api/hooks';
import { shipmentStatuses, type ShipmentStatus } from '../api/types';
import { formatDate, formatNumber } from '../components/format';
import { EmptyState, ErrorState, LoadingState } from '../components/states/QueryState';
import { StatusBadge } from '../components/StatusBadge';

const pageSize = 20;

export function ShipmentsPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<ShipmentStatus | null>(null);
  const [delayedOnly, setDelayedOnly] = useState(false);
  const { data, isPending, error } = useShipments({ page, pageSize, status, delayedOnly });

  return (
    <Stack gap="lg">
      <div>
        <Title order={1} fz={26}>
          Shipments
        </Title>
        <Text c="dimmed" size="sm">
          Parcels handed to carriers, tracked until delivery. Updates arrive from the carriers every few seconds.
        </Text>
      </div>

      <Card>
        <Group mb="md" gap="lg">
          <Select
            placeholder="All statuses"
            clearable
            data={shipmentStatuses}
            value={status}
            onChange={(value) => {
              setStatus(value as ShipmentStatus | null);
              setPage(1);
            }}
            aria-label="Filter by status"
            data-testid="shipments-status-filter"
            w={240}
          />
          <Switch
            label="Delayed only"
            checked={delayedOnly}
            onChange={(event) => {
              setDelayedOnly(event.currentTarget.checked);
              setPage(1);
            }}
            data-testid="shipments-delayed-only"
          />
        </Group>

        {isPending ? (
          <LoadingState label="Loading shipments…" />
        ) : error ? (
          <ErrorState error={error} title="Shipments unavailable" />
        ) : data.items.length === 0 ? (
          <EmptyState title="No shipments">A shipment opens when a fulfillment job is shipped.</EmptyState>
        ) : (
          <>
            <Table.ScrollContainer minWidth={860}>
              <Table highlightOnHover data-testid="shipments-table">
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Order</Table.Th>
                    <Table.Th>Carrier</Table.Th>
                    <Table.Th>Tracking</Table.Th>
                    <Table.Th>Destination</Table.Th>
                    <Table.Th>Status</Table.Th>
                    <Table.Th>Estimate</Table.Th>
                    <Table.Th>Promised</Table.Th>
                    <Table.Th ta="right">Open exceptions</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {data.items.map((shipment) => (
                    <Table.Tr key={shipment.id} data-testid="shipment-row" data-order-number={shipment.orderNumber}>
                      <Table.Td>
                        <Link to={`/shipments/${shipment.id}`}>{shipment.orderNumber}</Link>
                      </Table.Td>
                      <Table.Td>{shipment.carrierName}</Table.Td>
                      <Table.Td>
                        <Text size="sm" ff="monospace">
                          {shipment.trackingNumber ?? '—'}
                        </Text>
                      </Table.Td>
                      <Table.Td>
                        {shipment.destinationCity}, {shipment.destinationCountry}
                      </Table.Td>
                      <Table.Td>
                        <Group gap={6} wrap="nowrap">
                          <StatusBadge status={shipment.status} />
                          {shipment.isDelayed && (
                            <Badge color="orange" variant="filled" size="sm" data-testid="delayed-badge">
                              Delayed
                            </Badge>
                          )}
                        </Group>
                      </Table.Td>
                      <Table.Td>{formatDate(shipment.estimatedDelivery)}</Table.Td>
                      <Table.Td>{formatDate(shipment.promisedDeliveryDate)}</Table.Td>
                      <Table.Td ta="right">{formatNumber(shipment.openExceptions)}</Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
            <Group justify="space-between" mt="md">
              <Text size="sm" c="dimmed">
                {formatNumber(data.totalCount)} shipments
              </Text>
              <Pagination total={data.totalPages ?? 1} value={page} onChange={setPage} size="sm" />
            </Group>
          </>
        )}
      </Card>
    </Stack>
  );
}
