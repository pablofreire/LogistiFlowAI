import { Badge } from '@mantine/core';

const colors: Record<string, string> = {
  Created: 'gray',
  PendingInventory: 'yellow',
  Reserved: 'cyan',
  Picking: 'blue',
  Packed: 'indigo',
  Shipped: 'violet',
  Delivered: 'green',
  Cancelled: 'gray',
  Failed: 'red',
  Released: 'gray',
};

const labels: Record<string, string> = {
  PendingInventory: 'Pending inventory',
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <Badge color={colors[status] ?? 'gray'} variant="light" data-testid="status-badge" data-status={status}>
      {labels[status] ?? status}
    </Badge>
  );
}
