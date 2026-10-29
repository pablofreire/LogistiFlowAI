import { Alert, Center, Loader, Stack, Text } from '@mantine/core';
import { IconAlertTriangle, IconInbox } from '@tabler/icons-react';
import type { ReactNode } from 'react';
import { ApiError } from '../../api/envelope';

export function LoadingState({ label = 'Loading…' }: { label?: string }) {
  return (
    <Center py="xl" data-testid="loading-state">
      <Stack align="center" gap="xs">
        <Loader size="sm" />
        <Text size="sm" c="dimmed">
          {label}
        </Text>
      </Stack>
    </Center>
  );
}

export function ErrorState({ error, title = 'Something went wrong' }: { error: unknown; title?: string }) {
  const message = error instanceof ApiError ? error.message : 'The service did not respond. Try again in a moment.';
  const trace = error instanceof ApiError && error.traceId ? `Trace ${error.traceId}` : null;
  return (
    <Alert color="red" variant="light" icon={<IconAlertTriangle size={18} />} title={title} data-testid="error-state">
      {message}
      {trace && (
        <Text size="xs" c="dimmed" mt={4}>
          {trace}
        </Text>
      )}
    </Alert>
  );
}

export function EmptyState({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <Center py="xl" data-testid="empty-state">
      <Stack align="center" gap={6}>
        <IconInbox size={32} stroke={1.5} color="var(--mantine-color-dimmed)" />
        <Text fw={600}>{title}</Text>
        {children && (
          <Text size="sm" c="dimmed" ta="center" maw={360}>
            {children}
          </Text>
        )}
      </Stack>
    </Center>
  );
}
