import * as functionsV1 from 'firebase-functions/v1';
import { onSchedule } from 'firebase-functions/v2/scheduler';
import { logger } from 'firebase-functions/v2';
import type { OrderDoc } from '../shared/types.js';
import { Timestamp, db } from '../lib/firebase.js';
import { bumpStats, grantRef } from '../lib/records.js';

/** Counts new accounts for the dashboard (the game signs every player in anonymously on first launch). */
export const onAccountCreated = functionsV1.auth.user().onCreate(async () => {
  const batch = db.batch();
  bumpStats(batch, { newPlayers: 1 });
  await batch.commit();
});

const DAY = 86_400_000;
const PENDING_EXPIRY_DAYS = 3;

/**
 * Daily check that every paid order has its grant (logs an ERROR — set a Cloud Monitoring alert on it), and closes
 * checkouts that were never paid. A late "paid" webhook still grants for a canceled order.
 */
export const checkOrders = onSchedule({ schedule: 'every day 03:00', timeZone: 'UTC' }, async () => {
  const since = Timestamp.fromMillis(Date.now() - 2 * DAY);
  const paid = await db.collection('orders').where('status', '==', 'paid').where('paidAt', '>=', since).get();
  let missing = 0;
  for (const doc of paid.docs) {
    const order = doc.data() as OrderDoc<Timestamp>;
    if (!order.grantId || !(await grantRef(order.uid, order.grantId).get()).exists) {
      missing++;
      logger.error('Paid order without a grant', { orderId: doc.id, uid: order.uid, grantId: order.grantId });
    }
  }

  const stale = await db.collection('orders').where('status', '==', 'pending')
    .where('createdAt', '<', Timestamp.fromMillis(Date.now() - PENDING_EXPIRY_DAYS * DAY)).limit(500).get();
  const batch = db.batch();
  stale.docs.forEach((doc) => batch.update(doc.ref, { status: 'canceled', note: 'Checkout never completed.' }));
  await batch.commit();
  logger.info('order check done', { paid: paid.size, missing, expired: stale.size });
});
