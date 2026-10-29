import { Alert, Button, Group, Modal, Stack, Text, Textarea } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconAlertTriangle } from '@tabler/icons-react';
import { useState } from 'react';
import { useFloorAction, type FloorAction } from '../api/hooks';
import type { FulfillmentJobStatus } from '../api/types';
import { useAuth } from '../auth/useAuth';
import { ErrorState } from './states/QueryState';

/** The actions the state machine allows from each job status; the API enforces the same rules. */
const actionsByStatus: Partial<Record<FulfillmentJobStatus, FloorAction[]>> = {
  Picking: ['pack', 'report-issue'],
  Packed: ['ship', 'report-issue'],
  Failed: ['reprocess'],
};

const labels: Record<FloorAction, string> = {
  pack: 'Pack',
  ship: 'Ship',
  'report-issue': 'Report issue',
  reprocess: 'Reprocess',
};

/** Pack, ship, report an issue or reprocess a job. Hidden for roles that cannot operate the floor. */
export function FloorActions({ jobId, status, size = 'xs' }: { jobId: string; status: FulfillmentJobStatus; size?: 'xs' | 'sm' }) {
  const { hasRole } = useAuth();
  const action = useFloorAction();
  const [issueOpened, issue] = useDisclosure(false);
  const [reason, setReason] = useState('');

  const available = actionsByStatus[status] ?? [];
  if (!hasRole('Admin', 'OpsManager', 'WarehouseOperator') || available.length === 0) {
    return null;
  }

  const run = (floorAction: FloorAction) => {
    if (floorAction === 'report-issue') {
      issue.open();
      return;
    }

    action.mutate({ jobId, action: floorAction });
  };

  return (
    <>
      <Group gap={6} wrap="nowrap">
        {available.map((floorAction) => (
          <Button
            key={floorAction}
            size={size}
            variant={floorAction === 'report-issue' ? 'subtle' : 'light'}
            color={floorAction === 'report-issue' ? 'orange' : 'blue'}
            loading={action.isPending && action.variables?.action === floorAction}
            onClick={() => run(floorAction)}
            data-testid={`job-action-${floorAction}`}
          >
            {labels[floorAction]}
          </Button>
        ))}
      </Group>
      {action.error && !issueOpened && (
        <Alert color="red" variant="light" mt="xs" icon={<IconAlertTriangle size={16} />} data-testid="floor-action-error">
          {action.error.message}
        </Alert>
      )}
      <Modal opened={issueOpened} onClose={issue.close} title="Report an issue" centered>
        <Stack>
          <Text size="sm">The job goes on hold as Failed. It can be reprocessed once the problem is fixed.</Text>
          {action.error && <ErrorState error={action.error} title="Could not report the issue" />}
          <Textarea label="What happened?" required minRows={2} value={reason} onChange={(e) => setReason(e.currentTarget.value)} data-testid="issue-reason" />
          <Group justify="flex-end">
            <Button variant="default" onClick={issue.close}>
              Back
            </Button>
            <Button
              color="orange"
              loading={action.isPending}
              disabled={reason.trim().length < 3}
              onClick={() => action.mutate({ jobId, action: 'report-issue', reason }, { onSuccess: () => { issue.close(); setReason(''); } })}
              data-testid="confirm-issue"
            >
              Put on hold
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
