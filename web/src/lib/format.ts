import type { TimestampLike } from '@shared/types';

export type TimeInput = number | TimestampLike | null | undefined;

function toMillis(value: TimeInput): number | null {
  if (value === null || value === undefined) return null;
  return typeof value === 'number' ? value : value.toMillis();
}

export function formatDate(value: TimeInput, withTime = false): string {
  const ms = toMillis(value);
  if (ms === null) return '—';
  return new Date(ms).toLocaleString(undefined, withTime
    ? { dateStyle: 'medium', timeStyle: 'short' }
    : { dateStyle: 'medium' });
}

export function formatNumber(value: number): string {
  return value.toLocaleString();
}

/** "3 hours ago", "in 2 days". */
export function relativeTime(value: TimeInput): string {
  const ms = toMillis(value);
  if (ms === null) return '—';
  const diff = ms - Date.now();
  const units: [Intl.RelativeTimeFormatUnit, number][] = [['year', 31_536_000_000], ['month', 2_592_000_000], ['day', 86_400_000], ['hour', 3_600_000], ['minute', 60_000]];
  const format = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' });
  for (const [unit, size] of units) {
    if (Math.abs(diff) >= size || unit === 'minute') return format.format(Math.round(diff / size), unit);
  }
  return '';
}

/** "{count} gems" → fills {placeholders}. */
export function fill(template: string, values: Record<string, string | number>): string {
  return template.replace(/\{(\w+)\}/g, (_, key: string) => String(values[key] ?? `{${key}}`));
}

export function queryParam(name: string): string {
  return typeof window === 'undefined' ? '' : new URLSearchParams(window.location.search).get(name) ?? '';
}
