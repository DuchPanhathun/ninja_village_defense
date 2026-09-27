import { createHmac, timingSafeEqual } from 'node:crypto';

/** A provider's webhook, reduced to what the shop needs. Amounts are minor units. */
export interface PaymentEvent {
  type: 'paid' | 'refunded' | 'chargeback' | 'failed' | 'canceled';
  provider: string;
  /** The provider's payment/transaction id — unique per payment, used for the grant id (exactly-once). */
  paymentId: string;
  orderId: string;
  amount: number;
  currency: string;
}

export interface CheckoutOrder {
  id: string;
  amount: number;
  currency: string;
  productName: string;
  email: string | null;
}

export interface CheckoutSession {
  /** The provider's hosted checkout page (we never see card details). */
  url: string;
  providerRef: string | null;
}

export class WebhookSignatureError extends Error {}

/**
 * One payment provider. To go live, add an adapter here (Xsolla, Stripe, PayPal, a local QR provider...) that
 * creates the provider's hosted checkout and verifies its webhook signature, then set PAYMENTS_PROVIDER.
 * Everything after that — marking the order paid and creating the grant exactly once — is shared (apply.ts).
 */
export interface PaymentProvider {
  id: string;
  createCheckout(order: CheckoutOrder, urls: { site: string; success: string; cancel: string }): Promise<CheckoutSession>;
  /** Verifies the signature (throws WebhookSignatureError) and returns the events in the request. */
  parseWebhook(rawBody: Buffer, headers: Record<string, string | string[] | undefined>, secret: string): PaymentEvent[];
}

// ---------------------------------------------------------------- sandbox (test mode, no money)

export const SANDBOX_SIGNATURE_HEADER = 'x-sandbox-signature';
const SIGNATURE_TOLERANCE_S = 5 * 60;

function hmac(secret: string, timestamp: number, body: string): string {
  return createHmac('sha256', secret).update(`${timestamp}.${body}`).digest('hex');
}

/** `t=<unix seconds>,v1=<hex HMAC-SHA256 of "t.body">` — the same scheme as the big providers (replay window 5 min). */
export function signSandboxPayload(body: string, secret: string, timestamp = Math.floor(Date.now() / 1000)): string {
  return `t=${timestamp},v1=${hmac(secret, timestamp, body)}`;
}

export function verifySandboxSignature(body: string, header: string | undefined, secret: string, nowS = Math.floor(Date.now() / 1000)): boolean {
  if (!header || !secret) return false;
  const parts = Object.fromEntries(header.split(',').map((part) => part.split('=', 2) as [string, string]));
  const timestamp = Number(parts.t);
  if (!Number.isInteger(timestamp) || Math.abs(nowS - timestamp) > SIGNATURE_TOLERANCE_S || !parts.v1) return false;
  const expected = Buffer.from(hmac(secret, timestamp, body), 'hex');
  const given = Buffer.from(parts.v1, 'hex');
  return given.length === expected.length && timingSafeEqual(given, expected);
}

const EVENT_TYPES: readonly PaymentEvent['type'][] = ['paid', 'refunded', 'chargeback', 'failed', 'canceled'];

export const sandboxProvider: PaymentProvider = {
  id: 'sandbox',
  async createCheckout(order, urls) {
    // Our own test checkout page stands in for the provider's hosted page.
    return { url: `${urls.site}/shop/checkout/?order=${encodeURIComponent(order.id)}`, providerRef: `sbx_${order.id}` };
  },
  parseWebhook(rawBody, headers, secret) {
    const body = rawBody.toString('utf8');
    const header = headers[SANDBOX_SIGNATURE_HEADER];
    if (!verifySandboxSignature(body, Array.isArray(header) ? header[0] : header, secret)) throw new WebhookSignatureError('bad signature');
    const event = JSON.parse(body) as Partial<PaymentEvent>;
    if (!EVENT_TYPES.includes(event.type as PaymentEvent['type']) || typeof event.paymentId !== 'string' || typeof event.orderId !== 'string'
      || typeof event.amount !== 'number' || typeof event.currency !== 'string') {
      throw new WebhookSignatureError('malformed event');
    }
    return [{ type: event.type as PaymentEvent['type'], provider: 'sandbox', paymentId: event.paymentId, orderId: event.orderId, amount: event.amount, currency: event.currency }];
  },
};

const PROVIDERS: Record<string, PaymentProvider> = { sandbox: sandboxProvider };

export function getProvider(id: string): PaymentProvider {
  const provider = PROVIDERS[id];
  if (!provider) throw new Error(`Unknown PAYMENTS_PROVIDER "${id}"`);
  return provider;
}
