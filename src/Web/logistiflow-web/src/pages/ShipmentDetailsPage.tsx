import { Alert, Anchor, Button, Card, Group, SimpleGrid, Stack, Table, Text, Timeline, Title } from '@mantine/core';
import { IconAlertTriangle, IconArrowLeft, IconClockExclamation } from '@tabler/icons-react';
import { Link, useParams } from 'react-router-dom';
import { ApiError } from '../api/envelope';
import { useRetryBooking, useShipment } from '../api/hooks';
import type { ShipmentDetails } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { formatDate, formatDateTime, formatNumber } from '../components/format';
import { ResolveExceptionButton } from '../components/ResolveExceptionButton';
import { EmptyState, ErrorState, LoadingState } from '../components/states/QueryState';
import { StatusBadge } from '../components/StatusBadge';

export function ShipmentDetailsPage() {
  const { shipmentId = '' } = useParams();
  const shipment = useShipment(shipmentId);

  if (shipment.isPending) {
    return <LoadingState label="Loading shipment…" />;
  }

  if (shipment.error) {
    const notFound = shipment.error instanceof ApiError && shipment.error.status === 404;
    return <ErrorState error={shipment.error} title={notFound ? 'Shipment not found' : 'Shipment unavailable'} />;
  }

  const details = shipment.data;
  return (
    <Stack gap="lg">
      <Anchor component={Link} to="/shipments" size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} /> Shipments
        </Group>
      </Anchor>

      <Group justify="space-between" align="flex-start">
        <div>
          <Group gap="sm">
            <Title order={1} fz={26} data-testid="shipment-tracking-number">
              {details.trackingNumber ?? 'Awaiting booking'}
            </Title>
            <span data-testid="shipment-status">
              <StatusBadge status={details.status} />
            </span>
          </Group>
          <Text c="dimmed" size="sm">
            {details.carrierName} · order <Link to={`/orders/${details.orderId}`}>{details.orderNumber}</Link> · to {details.destinationCity},{' '}
            {details.destinationCountry}
          </Text>
        </div>
        {details.status === 'BookingFailed' && <RetryBookingButton shipmentId={details.id} />}
      </Group>

      {details.isDelayed && (
        <Alert color="orange" variant="light" icon={<IconClockExclamation size={18} />} data-testid="shipment-delayed-alert">
          Delayed: the carrier now expects delivery on {formatDate(details.estimatedDelivery)}
          {details.promisedDeliveryDate ? `, promised for ${formatDate(details.promisedDeliveryDate)}` : ''}.
        </Alert>
      )}

      <SimpleGrid cols={{ base: 1, md: 2 }}>
        <Card>
          <Title order={2} fz="md" mb="sm">
            Delivery
          </Title>
          <Stack gap="xs">
            <Row label="Promised" value={formatDate(details.promisedDeliveryDate)} />
            <Row label="First carrier estimate" value={formatDate(details.originalEstimatedDelivery)} />
            <Row label="Current estimate" value={formatDate(details.estimatedDelivery)} testId="shipment-estimate" />
            <Row label="Delivered" value={formatDateTime(details.deliveredAt)} />
          </Stack>
        </Card>
        <Card>
          <Title order={2} fz="md" mb="sm">
            Handover
          </Title>
          <Stack gap="xs">
            <Row label="Warehouse" value={details.warehouseCode} />
            <Row label="Carrier" value={`${details.carrierName} (${details.carrierCode})`} />
            <Row label="Booking attempts" value={formatNumber(details.bookingAttempts)} />
            <Row label="Last update" value={details.statusReason ?? '—'} />
          </Stack>
        </Card>
      </SimpleGrid>

      <ExceptionsCard details={details} />

      <Card>
        <Title order={2} fz="md" mb="md">
          Tracking
        </Title>
        {details.trackingEvents.length === 0 ? (
          <EmptyState title="No carrier events yet">Events appear once the carrier has the parcel.</EmptyState>
        ) : (
          <Timeline bulletSize={14} lineWidth={2} active={details.trackingEvents.length - 1} data-testid="tracking-timeline">
            {details.trackingEvents.map((event, index) => (
              <Timeline.Item key={index} title={event.description} data-testid="tracking-event" data-code={event.code}>
                <Text size="xs" c="dimmed">
                  {event.location} · {formatDateTime(event.occurredAt)}
                  {event.estimatedDelivery ? ` · estimate ${formatDate(event.estimatedDelivery)}` : ''}
                </Text>
              </Timeline.Item>
            ))}
          </Timeline>
        )}
      </Card>
    </Stack>
  );
}

function ExceptionsCard({ details }: { details: ShipmentDetails }) {
  return (
    <Card data-testid="shipment-exceptions">
      <Title order={2} fz="md" mb="md">
        Exceptions
      </Title>
      {details.exceptions.length === 0 ? (
        <Text size="sm" c="dimmed">
          None. Delays, failed attempts and carrier outages show up here.
        </Text>
      ) : (
        <Table.ScrollContainer minWidth={640}>
          <Table>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Type</Table.Th>
                <Table.Th>Details</Table.Th>
                <Table.Th>Status</Table.Th>
                <Table.Th>Opened</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {details.exceptions.map((exception) => (
                <Table.Tr key={exception.id} data-testid="shipment-exception" data-type={exception.type} data-status={exception.status}>
                  <Table.Td>
                    <StatusBadge status={exception.type} />
                  </Table.Td>
                  <Table.Td>
                    <Text size="sm">{exception.details}</Text>
                    {exception.resolution && (
                      <Text size="xs" c="dimmed">
                        Resolved: {exception.resolution}
                        {exception.resolvedBy && exception.resolvedBy !== 'system' ? ` (${exception.resolvedBy})` : ''}
                      </Text>
                    )}
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
      )}
    </Card>
  );
}

function RetryBookingButton({ shipmentId }: { shipmentId: string }) {
  const { hasRole } = useAuth();
  const retry = useRetryBooking(shipmentId);
  if (!hasRole('Admin', 'OpsManager')) {
    return null;
  }

  return (
    <Stack gap={4} align="flex-end">
      <Button color="blue" loading={retry.isPending} onClick={() => retry.mutate()} data-testid="retry-booking">
        Retry booking
      </Button>
      {retry.error && (
        <Text size="xs" c="red">
          <IconAlertTriangle size={12} /> {retry.error.message}
        </Text>
      )}
    </Stack>
  );
}

function Row({ label, value, testId }: { label: string; value: string; testId?: string }) {
  return (
    <Group justify="space-between" wrap="nowrap">
      <Text size="sm" c="dimmed">
        {label}
      </Text>
      <Text size="sm" fw={600} ta="right" data-testid={testId}>
        {value}
      </Text>
    </Group>
  );
}
