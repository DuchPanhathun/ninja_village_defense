import { logger } from 'firebase-functions/v2';
import type { GrantDoc, OrderDoc, ProductDoc } from '../shared/types.js';
import { Timestamp, db } from '../lib/firebase.js';
import { bumpStats, grantRef, newGrant, safeId } from '../lib/records.js';
import { userRef } from '../lib/players.js';
import type { PaymentEvent } from './providers.js';

export type ApplyResult = 'granted' | 'duplicate' | 'updated' | 'review' | 'unknown-order';

/** The grant for a payment is keyed by the provider's payment id, so the same payment can never grant twice. */
export function purchaseGrantId(provider: string, paymentId: string): string {
  return safeId(`purchase_${provider}_${paymentId}`);
}

/**
 * Applies one verified payment event to its order, in a transaction — the single path for webhooks and sandbox
 * payments alike:
 * - paid: marks the order paid and creates the purchase grant (exactly once; repeats are no-ops). A wrong amount or
 *   a second copy of a once-only offer puts the order in "review" instead of granting.
 * - refunded / chargeback: revokes the grant if the game hasn't collected it yet; otherwise flags the player for
 *   review (the gems may be spent — staff decide).
 * - failed / canceled: closes a pending order.
 */
export async function applyPaymentEvent(event: PaymentEvent): Promise<ApplyResult> {
  const orderRef = db.collection('orders').doc(event.orderId);
  const result = await db.runTransaction(async (tx): Promise<ApplyResult> => {
    const snap = await tx.get(orderRef);
    if (!snap.exists) return 'unknown-order';
    const order = snap.data() as OrderDoc<Timestamp>;
    const now = Timestamp.now();

    if (event.type === 'paid') {
      if (order.status === 'paid' || order.status === 'review' || order.status === 'refunded' || order.status === 'chargeback') return 'duplicate';
      if (event.amount !== order.amount || event.currency !== order.currency) {
        tx.update(orderRef, { status: 'review', paymentId: event.paymentId, note: `Paid ${event.amount} ${event.currency}, expected ${order.amount} ${order.currency}.` });
        return 'review';
      }
      const product = (await tx.get(db.collection('products').doc(order.productId))).data() as ProductDoc<Timestamp> | undefined;
      if (product?.oncePerPlayer) {
        const earlier = await tx.get(db.collection('orders').where('uid', '==', order.uid).where('productId', '==', order.productId).where('status', '==', 'paid').limit(1));
        if (!earlier.empty) {
          tx.update(orderRef, { status: 'review', paymentId: event.paymentId, note: 'Once-only offer bought twice: refund this order.' });
          return 'review';
        }
      }
      const grantId = purchaseGrantId(event.provider, event.paymentId);
      const ref = grantRef(order.uid, grantId);
      if ((await tx.get(ref)).exists) return 'duplicate';
      tx.create(ref, newGrant('purchase', order.rewards, 'Thanks for your purchase!', `${order.productName} from the web shop.`, { orderId: orderRef.id }));
      tx.update(orderRef, { status: 'paid', paymentId: event.paymentId, grantId, paidAt: now });
      bumpStats(tx, { orders: 1 }, { currency: order.currency, amount: order.amount });
      return 'granted';
    }

    if (event.type === 'refunded' || event.type === 'chargeback') {
      if (order.status === event.type) return 'duplicate';
      const wasPaid = order.status === 'paid';
      let note = order.note;
      if (wasPaid && order.grantId) {
        const ref = grantRef(order.uid, order.grantId);
        const grant = (await tx.get(ref)).data() as GrantDoc<Timestamp> | undefined;
        if (grant && grant.claimedAt === null) {
          tx.update(ref, { revokedAt: now });
          note = 'Refunded before the rewards were collected: grant revoked.';
        } else {
          tx.set(userRef(order.uid), { reviewFlag: { reason: event.type, orderId: orderRef.id, at: now } }, { merge: true });
          note = 'Refunded after the rewards were collected: player flagged for review.';
        }
      }
      tx.update(orderRef, { status: event.type, refundedAt: now, note });
      if (wasPaid) {
        bumpStats(tx, { refunds: 1 }, { currency: order.currency, amount: -order.amount });
      }
      return 'updated';
    }

    if (order.status !== 'pending') return 'duplicate';
    tx.update(orderRef, { status: event.type });
    return 'updated';
  });
  logger.info('payment event', { ...event, result });
  return result;
}
