import { onRequest } from 'firebase-functions/v2/https';
import { logger } from 'firebase-functions/v2';
import { defineString } from 'firebase-functions/params';
import { isRole } from '../shared/roles.js';
import type { OrderDoc, ProductDoc } from '../shared/types.js';
import { fail, playerCallable, str } from '../lib/callable.js';
import { PAYMENTS_PROVIDER, PAYMENT_WEBHOOK_SECRET, SITE_URL } from '../lib/config.js';
import { Timestamp, db } from '../lib/firebase.js';
import { consumeAttempt } from '../lib/rateLimit.js';
import { assertNotBanned, ownedInApp, readSaveStore, userRef } from '../lib/players.js';
import { applyPaymentEvent } from './apply.js';
import { WebhookSignatureError, getProvider } from './providers.js';

const HOUR = 60 * 60 * 1000;

/** Who may complete sandbox (test) payments: "staff" (testers get the viewer role) or "everyone". */
const SANDBOX_PAYMENTS_FOR = defineString('SANDBOX_PAYMENTS_FOR', { default: 'staff' });

/**
 * Starts a web purchase: the order is created here with the catalog price (never a price from the browser), then
 * the provider's hosted checkout takes over. Once-only offers are checked against earlier web orders and the
 * app purchases recorded in the cloud save.
 */
export const createCheckout = playerCallable('createCheckout', async (data, caller) => {
  if (caller.anonymous || !caller.email) fail('failed-precondition', 'link-email', 'Link an email to your game account first.');
  const productId = str(data.productId, 'Product', { min: 1, max: 64 });
  await consumeAttempt(`checkout_${caller.uid}`, 20, HOUR);

  const [productSnap, userSnap] = await db.getAll(db.collection('products').doc(productId), userRef(caller.uid));
  const product = productSnap.data() as ProductDoc<Timestamp> | undefined;
  if (!product || !product.active) fail('not-found', 'no-product', 'This offer is not available.');
  assertNotBanned(userSnap.data());

  if (product.oncePerPlayer) {
    const earlier = await db.collection('orders').where('uid', '==', caller.uid).where('productId', '==', productId).where('status', '==', 'paid').limit(1).get();
    const inApp = product.appProductId !== null && ownedInApp(readSaveStore(userSnap.data()?.save), product.appProductId);
    if (!earlier.empty || inApp) fail('already-exists', 'already-owned', 'You already have this offer.');
  }

  const provider = getProvider(PAYMENTS_PROVIDER.value());
  const orderRef = db.collection('orders').doc();
  const order: OrderDoc<Timestamp> = {
    uid: caller.uid,
    email: caller.email,
    productId,
    productName: product.name,
    rewards: product.rewards,
    amount: product.salePrice ?? product.price,
    currency: product.currency,
    status: 'pending',
    provider: provider.id,
    providerRef: null,
    paymentId: null,
    grantId: null,
    createdAt: Timestamp.now(),
    paidAt: null,
    refundedAt: null,
    note: '',
  };
  await orderRef.set(order);

  const site = SITE_URL.value().replace(/\/$/, '');
  const session = await provider.createCheckout(
    { id: orderRef.id, amount: order.amount, currency: order.currency, productName: order.productName, email: order.email },
    { site, success: `${site}/shop/done/?order=${orderRef.id}`, cancel: `${site}/shop/?canceled=${orderRef.id}` },
  );
  await orderRef.update({ providerRef: session.providerRef });
  return { orderId: orderRef.id, url: session.url };
});

/** Test mode only: the sandbox checkout page completes (or cancels) the caller's own pending order. */
export const sandboxPay = playerCallable('sandboxPay', async (data, caller) => {
  if (PAYMENTS_PROVIDER.value() !== 'sandbox') fail('failed-precondition', 'not-sandbox', 'Test payments are off.');
  if (SANDBOX_PAYMENTS_FOR.value() !== 'everyone' && !isRole(caller.token.role))
    fail('permission-denied', 'not-tester', 'Test payments are only open to testers (staff accounts).');

  const orderId = str(data.orderId, 'Order', { min: 1, max: 64 });
  const snap = await db.collection('orders').doc(orderId).get();
  const order = snap.data() as OrderDoc<Timestamp> | undefined;
  if (!order || order.uid !== caller.uid) fail('not-found', 'no-order', 'Order not found.');
  if (order.status !== 'pending') return { status: order.status };

  const outcome = data.outcome === 'paid' ? 'paid' : 'canceled';
  await applyPaymentEvent({ type: outcome, provider: 'sandbox', paymentId: `sbx_${orderId}`, orderId, amount: order.amount, currency: order.currency });
  const after = (await snap.ref.get()).data() as OrderDoc<Timestamp>;
  return { status: after.status };
});

/**
 * The payment provider's webhook (POST). Verifies the signature, then applies each event exactly once.
 * Answers 2xx for duplicates and unknown orders (so the provider stops retrying) and 5xx on errors (so it retries).
 */
export const paymentWebhook = onRequest({ secrets: [PAYMENT_WEBHOOK_SECRET] }, async (req, res) => {
  if (req.method !== 'POST') {
    res.status(405).send('POST only');
    return;
  }
  try {
    const provider = getProvider(PAYMENTS_PROVIDER.value());
    const events = provider.parseWebhook(req.rawBody, req.headers, PAYMENT_WEBHOOK_SECRET.value());
    const results = [];
    for (const event of events) results.push(await applyPaymentEvent(event));
    res.status(200).json({ results });
  } catch (error) {
    if (error instanceof WebhookSignatureError || error instanceof SyntaxError) {
      logger.warn('webhook rejected', { reason: error.message });
      res.status(400).send('invalid webhook');
      return;
    }
    logger.error('webhook failed', { error: error instanceof Error ? error.stack : String(error) });
    res.status(500).send('error');
  }
});
