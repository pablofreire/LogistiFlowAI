import { Alert, Card, Group, Progress, SimpleGrid, Stack, Table, Text, Title } from '@mantine/core';
import {
  IconAlertOctagon,
  IconAlertTriangle,
  IconBox,
  IconCircleCheck,
  IconClockExclamation,
  IconClockHour4,
  IconPackage,
  IconTruck,
  IconTruckDelivery,
} from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { useDashboard } from '../api/hooks';
import { orderStatuses } from '../api/types';
import { formatDateTime, formatDuration, formatNumber, formatSource } from '../components/format';
import { KpiCard } from '../components/KpiCard';
import { EmptyState, ErrorState, LoadingState } from '../components/states/QueryState';
import { StatusBadge } from '../components/StatusBadge';

export function DashboardPage() {
  const { data, isPending, error } = useDashboard();

  if (isPending) {
    return <LoadingState label="Loading operations metrics…" />;
  }

  if (error) {
    return <ErrorState error={error} title="Dashboard unavailable" />;
  }

  const { orders, inventory, fulfillment, shipping } = data;
  const byStatus = orders?.byStatus ?? {};
  const totalOrders = orders?.totalOrders ?? 0;

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-end">
        <div>
          <Title order={1} fz={26}>
            Operations dashboard
          </Title>
          <Text c="dimmed" size="sm">
            Live from the Orders, Inventory, Fulfillment and Shipping services · updated {formatDateTime(data.generatedAt)}
          </Text>
        </div>
      </Group>

      {data.unavailableSources.length > 0 && (
        <Alert color="yellow" variant="light" icon={<IconAlertTriangle size={18} />} data-testid="dashboard-degraded">
          Partial data: {data.unavailableSources.join(', ')} did not respond. Other sections are current.
        </Alert>
      )}

      <SimpleGrid cols={{ base: 1, xs: 2, lg: 4 }}>
        <KpiCard testId="kpi-total-orders" label="Total orders" value={formatNumber(orders?.totalOrders)} hint={`${formatNumber(orders?.createdLast24Hours)} in the last 24 h`} icon={<IconPackage size={22} />} />
        <KpiCard testId="kpi-fulfillment-jobs" label="Fulfillment jobs" value={formatNumber(fulfillment?.totalJobs)} hint={`${formatNumber(fulfillment?.byStatus.Picking)} picking now`} icon={<IconTruckDelivery size={22} />} />
        <KpiCard testId="kpi-reserved-units" label="Reserved units" value={formatNumber(inventory?.totalReserved)} hint={`${formatNumber(inventory?.totalAvailable)} available`} icon={<IconBox size={22} />} />
        <KpiCard
          testId="kpi-processing-time"
          label="Avg. processing time"
          value={formatDuration(orders?.averageSecondsToDelivered)}
          hint={`Created → delivered · ${formatDuration(orders?.averageSecondsToShipped)} to ship`}
          icon={<IconClockHour4 size={22} />}
        />
      </SimpleGrid>

      <SimpleGrid cols={{ base: 1, xs: 2, lg: 4 }}>
        <KpiCard testId="kpi-in-transit" label="With carriers" value={formatNumber(shipping?.inTransit)} hint={`${formatNumber(shipping?.totalShipments)} shipments in total`} icon={<IconTruck size={22} />} />
        <KpiCard testId="kpi-delivered" label="Delivered" value={formatNumber(shipping?.delivered)} hint={`${formatNumber(shipping?.byStatus.ReturnedToSender)} returned to sender`} icon={<IconCircleCheck size={22} />} />
        <KpiCard testId="kpi-delayed-shipments" label="Delayed shipments" value={formatNumber(shipping?.delayedShipments)} hint="Undelivered, with an open delay" icon={<IconClockExclamation size={22} />} />
        <KpiCard testId="kpi-open-exceptions" label="Open exceptions" value={formatNumber(shipping?.openExceptions)} hint={`${formatNumber(shipping?.openExceptionsByType.CarrierUnavailable)} carrier outages`} icon={<IconAlertOctagon size={22} />} />
      </SimpleGrid>

      <SimpleGrid cols={{ base: 1, md: 2 }}>
        <Card data-testid="orders-by-status">
          <Title order={2} fz="md" mb="md">
            Orders by status
          </Title>
          <Stack gap="sm">
            {orderStatuses.map((status) => {
              const count = byStatus[status] ?? 0;
              return (
                <div key={status} data-testid={`status-count-${status}`}>
                  <Group justify="space-between" mb={4}>
                    <StatusBadge status={status} />
                    <Text size="sm" fw={600} data-testid={`status-count-${status}-value`}>
                      {formatNumber(count)}
                    </Text>
                  </Group>
                  <Progress value={totalOrders === 0 ? 0 : (count / totalOrders) * 100} size="sm" aria-label={`${status} share`} />
                </div>
              );
            })}
          </Stack>
        </Card>

        <Card data-testid="warehouse-load">
          <Title order={2} fz="md" mb="md">
            Inventory and warehouses
          </Title>
          <SimpleGrid cols={3} mb="md">
            <Metric label="On hand" value={inventory?.totalOnHand} />
            <Metric label="Reserved" value={inventory?.totalReserved} />
            <Metric label="Low-stock positions" value={inventory?.lowStockPositions} />
          </SimpleGrid>
          {fulfillment && fulfillment.byWarehouse.length > 0 ? (
            <Table.ScrollContainer minWidth={320}>
              <Table striped highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Warehouse</Table.Th>
                    <Table.Th ta="right">Open jobs</Table.Th>
                    <Table.Th ta="right">Failed</Table.Th>
                    <Table.Th ta="right">Total</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {fulfillment.byWarehouse.map((row) => (
                    <Table.Tr key={row.warehouseCode}>
                      <Table.Td>{row.warehouseCode}</Table.Td>
                      <Table.Td ta="right">{formatNumber(row.open)}</Table.Td>
                      <Table.Td ta="right">{formatNumber(row.failed)}</Table.Td>
                      <Table.Td ta="right">{formatNumber(row.total)}</Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
          ) : (
            <EmptyState title="No fulfillment jobs yet">Jobs appear here once an order is reserved.</EmptyState>
          )}
        </Card>
      </SimpleGrid>

      <Card data-testid="recent-activity">
        <Title order={2} fz="md" mb="md">
          Recent order events
        </Title>
        {orders && orders.recentActivity.length > 0 ? (
          <Table.ScrollContainer minWidth={560}>
            <Table highlightOnHover>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Order</Table.Th>
                  <Table.Th>Status</Table.Th>
                  <Table.Th>Cause</Table.Th>
                  <Table.Th>When</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {orders.recentActivity.map((activity, index) => (
                  <Table.Tr key={`${activity.orderId}-${index}`}>
                    <Table.Td>
                      <Link to={`/orders/${activity.orderId}`}>{activity.orderNumber}</Link>
                    </Table.Td>
                    <Table.Td>
                      <StatusBadge status={activity.toStatus} />
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">{formatSource(activity.source)}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {formatDateTime(activity.occurredAt)}
                      </Text>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Table.ScrollContainer>
        ) : (
          <EmptyState title="No activity yet">Create an order to see the flow move through the services.</EmptyState>
        )}
      </Card>
    </Stack>
  );
}

function Metric({ label, value }: { label: string; value: number | null | undefined }) {
  return (
    <div>
      <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
        {label}
      </Text>
      <Text fw={700} fz="lg">
        {formatNumber(value)}
      </Text>
    </div>
  );
}
