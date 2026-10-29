import { Alert, Anchor, Button, Card, Group, Modal, SimpleGrid, Stack, Table, Text, Textarea, Timeline, Title } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconAlertTriangle, IconArrowLeft, IconCircleCheck } from '@tabler/icons-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ApiError } from '../api/envelope';
import { useCancelOrder, useFulfillmentJob, useOrder, useOrderHistory, useReservation } from '../api/hooks';
import { useAuth } from '../auth/useAuth';
import { formatDateTime, formatSource } from '../components/format';
import { ErrorState, LoadingState } from '../components/states/QueryState';
import { StatusBadge } from '../components/StatusBadge';

const cancellable = new Set(['Created', 'PendingInventory', 'Reserved']);

export function OrderDetailsPage() {
  const { orderId = '' } = useParams();
  const { hasRole } = useAuth();
  const order = useOrder(orderId);
  const status = order.data?.status;
  const history = useOrderHistory(orderId, status);
  const reservation = useReservation(orderId, status);
  const job = useFulfillmentJob(orderId, status);

  if (order.isPending) {
    return <LoadingState label="Loading order…" />;
  }

  if (order.error) {
    const notFound = order.error instanceof ApiError && order.error.status === 404;
    return <ErrorState error={order.error} title={notFound ? 'Order not found' : 'Order unavailable'} />;
  }

  const details = order.data;
  return (
    <Stack gap="lg">
      <Anchor component={Link} to="/orders" size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} /> Orders
        </Group>
      </Anchor>

      <Group justify="space-between" align="flex-start">
        <div>
          <Group gap="sm">
            <Title order={1} fz={26} data-testid="order-number">
              {details.orderNumber}
            </Title>
            <span data-testid="order-status">
              <StatusBadge status={details.status} />
            </span>
          </Group>
          <Text c="dimmed" size="sm">
            {details.customerName} · {details.destinationCity}, {details.destinationCountry} · received {formatDateTime(details.createdAt)}
          </Text>
        </div>
        {hasRole('Admin', 'OpsManager') && cancellable.has(details.status) && <CancelOrderButton orderId={details.id} />}
      </Group>

      {details.statusReason && (
        <Alert
          color={details.status === 'Failed' ? 'red' : 'blue'}
          variant="light"
          icon={details.status === 'Failed' ? <IconAlertTriangle size={18} /> : <IconCircleCheck size={18} />}
          data-testid="order-status-reason"
        >
          {details.statusReason}
        </Alert>
      )}

      <SimpleGrid cols={{ base: 1, md: 2 }}>
        <Card>
          <Title order={2} fz="md" mb="sm">
            Lines
          </Title>
          <Table data-testid="order-lines">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>#</Table.Th>
                <Table.Th>SKU</Table.Th>
                <Table.Th ta="right">Quantity</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {details.lines.map((line) => (
                <Table.Tr key={line.lineNumber}>
                  <Table.Td>{line.lineNumber}</Table.Td>
                  <Table.Td>{line.sku}</Table.Td>
                  <Table.Td ta="right">{line.quantity}</Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Card>

        <Card>
          <Title order={2} fz="md" mb="sm">
            Inventory and fulfillment
          </Title>
          <Stack gap="xs">
            <Group justify="space-between">
              <Text size="sm" c="dimmed">
                Reservation
              </Text>
              <span data-testid="reservation-status">
                {reservation.data ? <StatusBadge status={reservation.data.status} /> : <Text size="sm">Not yet</Text>}
              </span>
            </Group>
            <Group justify="space-between">
              <Text size="sm" c="dimmed">
                Warehouse
              </Text>
              <Text size="sm" fw={600} data-testid="reservation-warehouse">
                {reservation.data?.warehouseCode ?? '—'}
              </Text>
            </Group>
            <Group justify="space-between">
              <Text size="sm" c="dimmed">
                Fulfillment job
              </Text>
              <span data-testid="fulfillment-status">
                {job.data ? <StatusBadge status={job.data.status} /> : <Text size="sm">Not started</Text>}
              </span>
            </Group>
          </Stack>
        </Card>
      </SimpleGrid>

      <Card>
        <Title order={2} fz="md" mb="md">
          Timeline
        </Title>
        {history.isPending ? (
          <LoadingState />
        ) : (
          <Timeline bulletSize={14} lineWidth={2} active={(history.data?.length ?? 1) - 1} data-testid="order-timeline">
            {history.data?.map((change, index) => (
              <Timeline.Item key={index} title={<StatusBadge status={change.toStatus} />} data-testid="timeline-entry" data-status={change.toStatus}>
                <Text size="sm">{change.reason}</Text>
                <Text size="xs" c="dimmed">
                  {formatSource(change.source)} · {formatDateTime(change.occurredAt)}
                </Text>
              </Timeline.Item>
            ))}
          </Timeline>
        )}
      </Card>
    </Stack>
  );
}

function CancelOrderButton({ orderId }: { orderId: string }) {
  const [opened, { open, close }] = useDisclosure(false);
  const [reason, setReason] = useState('');
  const cancel = useCancelOrder(orderId);

  return (
    <>
      <Button color="red" variant="light" onClick={open} data-testid="cancel-order">
        Cancel order
      </Button>
      <Modal opened={opened} onClose={close} title="Cancel order" centered>
        <Stack>
          <Text size="sm">Held inventory is released and any fulfillment job is cancelled.</Text>
          {cancel.error && <ErrorState error={cancel.error} title="Could not cancel" />}
          <Textarea label="Reason" required minRows={2} value={reason} onChange={(e) => setReason(e.currentTarget.value)} data-testid="cancel-reason" />
          <Group justify="flex-end">
            <Button variant="default" onClick={close}>
              Keep order
            </Button>
            <Button color="red" loading={cancel.isPending} disabled={reason.trim().length < 3} onClick={() => cancel.mutate(reason, { onSuccess: close })} data-testid="confirm-cancel">
              Cancel order
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
