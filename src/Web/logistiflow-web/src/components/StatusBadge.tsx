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
  Consumed: 'green',
  PendingBooking: 'yellow',
  BookingFailed: 'red',
  LabelCreated: 'cyan',
  InTransit: 'blue',
  OutForDelivery: 'indigo',
  DeliveryAttemptFailed: 'orange',
  ReturnedToSender: 'red',
  Open: 'orange',
  Resolved: 'green',
  CarrierUnavailable: 'red',
  Delay: 'orange',
};

const labels: Record<string, string> = {
  PendingInventory: 'Pending inventory',
  PendingBooking: 'Pending booking',
  BookingFailed: 'Booking failed',
  LabelCreated: 'Label created',
  InTransit: 'In transit',
  OutForDelivery: 'Out for delivery',
  DeliveryAttemptFailed: 'Attempt failed',
  ReturnedToSender: 'Returned to sender',
  CarrierUnavailable: 'Carrier unavailable',
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <Badge color={colors[status] ?? 'gray'} variant="light" data-testid="status-badge" data-status={status}>
      {labels[status] ?? status}
    </Badge>
  );
}
