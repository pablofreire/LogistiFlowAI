import { ActionIcon, Alert, Button, Card, Group, NumberInput, Stack, Text, TextInput, Title } from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { IconAlertCircle, IconPlus, IconTrash } from '@tabler/icons-react';
import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError } from '../api/envelope';
import { useCreateOrder } from '../api/hooks';

interface LineDraft {
  key: number;
  sku: string;
  quantity: number;
}

export function CreateOrderPage() {
  const navigate = useNavigate();
  const createOrder = useCreateOrder();
  // One key per form instance: a double click or a retry after a timeout cannot create a second order.
  const [idempotencyKey] = useState(() => crypto.randomUUID());
  const [customerName, setCustomerName] = useState('');
  const [destinationCity, setDestinationCity] = useState('');
  const [destinationCountry, setDestinationCountry] = useState('US');
  const [lines, setLines] = useState<LineDraft[]>([{ key: 1, sku: '', quantity: 1 }]);

  const error = createOrder.error instanceof ApiError ? createOrder.error : null;
  const fieldError = (name: string) => error?.details?.[name]?.[0];

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const order = await createOrder
      .mutateAsync({
        idempotencyKey,
        body: {
          customerName,
          destinationCity,
          destinationCountry,
          promisedDeliveryDate: null,
          lines: lines.map((line) => ({ sku: line.sku.trim().toUpperCase(), quantity: line.quantity })),
        },
      })
      .catch(() => null);

    if (order) {
      notifications.show({ color: 'green', title: 'Order received', message: `${order.orderNumber} is waiting for inventory.` });
      navigate(`/orders/${order.id}`);
    }
  };

  const updateLine = (key: number, patch: Partial<LineDraft>) =>
    setLines((current) => current.map((line) => (line.key === key ? { ...line, ...patch } : line)));

  return (
    <Stack gap="lg" maw={760}>
      <div>
        <Title order={1} fz={26}>
          New order
        </Title>
        <Text c="dimmed" size="sm">
          Inventory is reserved automatically after the order is accepted.
        </Text>
      </div>

      <Card component="form" onSubmit={submit} noValidate>
        <Stack>
          {error && (
            <Alert color="red" variant="light" icon={<IconAlertCircle size={18} />} data-testid="create-order-error">
              {error.message}
            </Alert>
          )}
          <TextInput label="Customer" required value={customerName} onChange={(e) => setCustomerName(e.currentTarget.value)} error={fieldError('CustomerName')} data-testid="order-customer" />
          <Group grow wrap="wrap">
            <TextInput label="Destination city" required value={destinationCity} onChange={(e) => setDestinationCity(e.currentTarget.value)} error={fieldError('DestinationCity')} data-testid="order-city" />
            <TextInput label="Country (ISO code)" required maxLength={2} value={destinationCountry} onChange={(e) => setDestinationCountry(e.currentTarget.value.toUpperCase())} error={fieldError('DestinationCountry')} data-testid="order-country" />
          </Group>

          <Text fw={600} size="sm" mt="xs">
            Lines
          </Text>
          {lines.map((line, index) => (
            <Group key={line.key} align="flex-end" wrap="nowrap" data-testid="order-line">
              <TextInput
                label={index === 0 ? 'SKU' : undefined}
                placeholder="SKU-1001"
                required
                value={line.sku}
                onChange={(e) => updateLine(line.key, { sku: e.currentTarget.value })}
                error={fieldError(`Lines[${index}].Sku`)}
                style={{ flex: 1 }}
                aria-label={`SKU line ${index + 1}`}
                data-testid={`order-line-sku-${index}`}
              />
              <NumberInput
                label={index === 0 ? 'Quantity' : undefined}
                min={1}
                max={10000}
                value={line.quantity}
                onChange={(value) => updateLine(line.key, { quantity: Number(value) || 1 })}
                error={fieldError(`Lines[${index}].Quantity`)}
                w={120}
                aria-label={`Quantity line ${index + 1}`}
                data-testid={`order-line-quantity-${index}`}
              />
              <ActionIcon
                variant="subtle"
                color="red"
                size="lg"
                disabled={lines.length === 1}
                onClick={() => setLines((current) => current.filter((item) => item.key !== line.key))}
                aria-label={`Remove line ${index + 1}`}
              >
                <IconTrash size={18} />
              </ActionIcon>
            </Group>
          ))}
          <Group justify="space-between" mt="xs">
            <Button
              variant="subtle"
              leftSection={<IconPlus size={16} />}
              onClick={() => setLines((current) => [...current, { key: Date.now(), sku: '', quantity: 1 }])}
              data-testid="add-line"
            >
              Add line
            </Button>
            <Group>
              <Button variant="default" onClick={() => navigate('/orders')}>
                Cancel
              </Button>
              <Button type="submit" loading={createOrder.isPending} data-testid="submit-order">
                Create order
              </Button>
            </Group>
          </Group>
        </Stack>
      </Card>
    </Stack>
  );
}
