const dateTime = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' });
const number = new Intl.NumberFormat('en-US');

export function formatDateTime(value: string | null | undefined): string {
  return value ? dateTime.format(new Date(value)) : '—';
}

export function formatNumber(value: number | null | undefined): string {
  return value === null || value === undefined ? '—' : number.format(value);
}

export function formatDuration(seconds: number | null | undefined): string {
  if (seconds === null || seconds === undefined) {
    return '—';
  }

  return seconds < 60 ? `${seconds.toFixed(1)} s` : `${(seconds / 60).toFixed(1)} min`;
}

/** "orders.order-created" → "Order created"; "api" → "API". */
export function formatSource(source: string): string {
  if (source === 'api') {
    return 'API';
  }

  const name = source.split('.').pop() ?? source;
  const text = name.replaceAll('-', ' ');
  return text.charAt(0).toUpperCase() + text.slice(1);
}
