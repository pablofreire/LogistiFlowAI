import { Button, Group, Modal, Stack, Text, Textarea } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { useState } from 'react';
import { useResolveException } from '../api/hooks';
import { useAuth } from '../auth/useAuth';
import { ErrorState } from './states/QueryState';

/** Closes an open shipment exception with a note. Only roles holding shipments.manage see it. */
export function ResolveExceptionButton({ exceptionId }: { exceptionId: string }) {
  const { hasRole } = useAuth();
  const [opened, { open, close }] = useDisclosure(false);
  const [resolution, setResolution] = useState('');
  const resolve = useResolveException();

  if (!hasRole('Admin', 'OpsManager')) {
    return null;
  }

  return (
    <>
      <Button size="xs" variant="light" color="green" onClick={open} data-testid="resolve-exception">
        Resolve
      </Button>
      <Modal opened={opened} onClose={close} title="Resolve exception" centered>
        <Stack>
          <Text size="sm">Record what was done. The note is kept with the exception.</Text>
          {resolve.error && <ErrorState error={resolve.error} title="Could not resolve" />}
          <Textarea label="Resolution" required minRows={2} value={resolution} onChange={(e) => setResolution(e.currentTarget.value)} data-testid="resolution" />
          <Group justify="flex-end">
            <Button variant="default" onClick={close}>
              Back
            </Button>
            <Button
              color="green"
              loading={resolve.isPending}
              disabled={resolution.trim().length < 3}
              onClick={() => resolve.mutate({ exceptionId, resolution }, { onSuccess: close })}
              data-testid="confirm-resolve"
            >
              Resolve
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  );
}
