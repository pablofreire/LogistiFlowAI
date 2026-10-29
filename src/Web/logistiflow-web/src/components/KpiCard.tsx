import { Card, Group, Text, ThemeIcon } from '@mantine/core';
import type { ReactNode } from 'react';

interface KpiCardProps {
  label: string;
  value: string | number | null | undefined;
  hint?: string;
  icon: ReactNode;
  testId: string;
}

export function KpiCard({ label, value, hint, icon, testId }: KpiCardProps) {
  return (
    <Card data-testid={testId}>
      <Group justify="space-between" align="flex-start" wrap="nowrap">
        <div>
          <Text size="xs" tt="uppercase" fw={600} c="dimmed" lts={0.4}>
            {label}
          </Text>
          <Text fz={28} fw={700} mt={4} data-testid={`${testId}-value`}>
            {value ?? '—'}
          </Text>
          {hint && (
            <Text size="xs" c="dimmed" mt={2}>
              {hint}
            </Text>
          )}
        </div>
        <ThemeIcon size={40} radius="md" variant="light" color="navy">
          {icon}
        </ThemeIcon>
      </Group>
    </Card>
  );
}
