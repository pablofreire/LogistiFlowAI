import { Button, Stack, Text, ThemeIcon, Title } from '@mantine/core';
import { IconLock } from '@tabler/icons-react';
import { Link } from 'react-router-dom';

export function AccessDenied() {
  return (
    <Stack align="center" gap="sm" py={64} data-testid="access-denied">
      <ThemeIcon size={56} radius="xl" variant="light" color="red">
        <IconLock size={28} />
      </ThemeIcon>
      <Title order={2}>Access denied</Title>
      <Text c="dimmed" ta="center" maw={420}>
        Your role does not allow this action. Ask an operations manager if you need access.
      </Text>
      <Button component={Link} to="/" variant="light">
        Back to dashboard
      </Button>
    </Stack>
  );
}
