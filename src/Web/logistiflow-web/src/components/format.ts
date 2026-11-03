const dateTime = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' });
const number = new Intl.NumberFormat('en-US');
const calendarDate = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeZone: 'UTC' });

/** A calendar date from the API ("2026-11-04"), shown without any time-zone shift. */
export function formatDate(value: string | null | undefined): string {
  return value ? calendarDate.format(new Date(`${value}T00:00:00Z`)) : '—';
}

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

  if (seconds < 60) {
    return `${seconds.toFixed(1)} s`;
  }

  return seconds < 3600 ? `${(seconds / 60).toFixed(1)} min` : `${(seconds / 3600).toFixed(1)} h`;
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
