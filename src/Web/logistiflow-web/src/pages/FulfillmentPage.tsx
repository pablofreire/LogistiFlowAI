import { Card, Group, Pagination, Select, Stack, Table, Text, Title } from '@mantine/core';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useFulfillmentJobs } from '../api/hooks';
import { fulfillmentJobStatuses, type FulfillmentJobStatus } from '../api/types';
import { FloorActions } from '../components/FloorActions';
import { formatDateTime, formatNumber } from '../components/format';
import { EmptyState, ErrorState, LoadingState } from '../components/states/QueryState';
import { StatusBadge } from '../components/StatusBadge';

const pageSize = 20;

export function FulfillmentPage() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<FulfillmentJobStatus | null>(null);
  const { data, isPending, error } = useFulfillmentJobs({ page, pageSize, status });

  return (
    <Stack gap="lg">
      <div>
        <Title order={1} fz={26}>
          Fulfillment
        </Title>
        <Text c="dimmed" size="sm">
          Warehouse jobs, newest first. Pack and ship from here; a problem puts the job on hold until it is reprocessed.
        </Text>
      </div>

      <Card>
        <Group mb="md">
          <Select
            placeholder="All statuses"
            clearable
            data={fulfillmentJobStatuses}
            value={status}
            onChange={(value) => {
              setStatus(value as FulfillmentJobStatus | null);
              setPage(1);
            }}
            aria-label="Filter by status"
            data-testid="jobs-status-filter"
            w={220}
          />
        </Group>

        {isPending ? (
          <LoadingState label="Loading jobs…" />
        ) : error ? (
          <ErrorState error={error} title="Jobs unavailable" />
        ) : data.items.length === 0 ? (
          <EmptyState title="No jobs">Jobs are created when an order's stock is reserved.</EmptyState>
        ) : (
          <>
            <Table.ScrollContainer minWidth={720}>
              <Table highlightOnHover data-testid="jobs-table">
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Order</Table.Th>
                    <Table.Th>Warehouse</Table.Th>
                    <Table.Th ta="right">Units</Table.Th>
                    <Table.Th>Status</Table.Th>
                    <Table.Th>Updated</Table.Th>
                    <Table.Th>Actions</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {data.items.map((job) => (
                    <Table.Tr key={job.id} data-testid="job-row" data-order-number={job.orderNumber}>
                      <Table.Td>
                        <Link to={`/orders/${job.orderId}`}>{job.orderNumber}</Link>
                      </Table.Td>
                      <Table.Td>{job.warehouseCode}</Table.Td>
                      <Table.Td ta="right">{formatNumber(job.totalUnits)}</Table.Td>
                      <Table.Td>
                        <StatusBadge status={job.status} />
                        {job.status === 'Failed' && job.statusReason && (
                          <Text size="xs" c="dimmed" mt={2}>
                            {job.statusReason}
                          </Text>
                        )}
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" c="dimmed">
                          {formatDateTime(job.updatedAt)}
                        </Text>
                      </Table.Td>
                      <Table.Td>
                        <FloorActions jobId={job.id} status={job.status} />
                      </Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
            <Group justify="space-between" mt="md">
              <Text size="sm" c="dimmed">
                {formatNumber(data.totalCount)} jobs
              </Text>
              <Pagination total={data.totalPages ?? 1} value={page} onChange={setPage} size="sm" />
            </Group>
          </>
        )}
      </Card>
    </Stack>
  );
}
