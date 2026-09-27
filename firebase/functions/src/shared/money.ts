/** Prices are stored as whole minor units (cents for USD, yen for JPY) so no float ever touches money. */

export function currencyDigits(currency: string): number {
  try {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency }).resolvedOptions().maximumFractionDigits ?? 2;
  } catch {
    return 2;
  }
}

export function isCurrencyCode(value: unknown): value is string {
  if (typeof value !== 'string' || !/^[A-Z]{3}$/.test(value)) return false;
  try {
    new Intl.NumberFormat('en-US', { style: 'currency', currency: value });
    return true;
  } catch {
    return false;
  }
}

export function formatMoney(minor: number, currency: string, locale = 'en-US'): string {
  const digits = currencyDigits(currency);
  return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(minor / 10 ** digits);
}

/** "4.99" → 499 (USD). Returns null for anything that isn't a plain non-negative amount. */
export function parseMoney(text: string, currency: string): number | null {
  const digits = currencyDigits(currency);
  const clean = text.trim();
  const pattern = digits === 0 ? /^\d+$/ : new RegExp(`^\\d+(\\.\\d{1,${digits}})?$`);
  if (!pattern.test(clean)) return null;
  const [whole, fraction = ''] = clean.split('.');
  return Number(whole) * 10 ** digits + Number(fraction.padEnd(digits, '0') || 0);
}

export function minorToInput(minor: number, currency: string): string {
  const digits = currencyDigits(currency);
  return digits === 0 ? String(minor) : (minor / 10 ** digits).toFixed(digits);
}
