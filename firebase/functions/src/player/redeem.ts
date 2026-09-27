import { logger } from 'firebase-functions/v2';
import { evaluateCode, isValidCode, normalizeCode } from '../shared/codes.js';
import type { CodeDoc, CodeRedemptionDoc, MyRedemptionDoc } from '../shared/types.js';
import { fail, playerCallable } from '../lib/callable.js';
import { REDEEM_ATTEMPTS_PER_HOUR } from '../lib/config.js';
import { FieldValue, Timestamp, db, millis } from '../lib/firebase.js';
import { consumeAttempt } from '../lib/rateLimit.js';
import { bumpStats, grantRef, newGrant } from '../lib/records.js';
import { assertNotBanned, userRef } from '../lib/players.js';

const HOUR = 60 * 60 * 1000;
const INVALID = 'This code isn’t valid. Check it and try again.';

/**
 * Redeems a code for the caller, all in one transaction: checks the code (exists, active, started, not expired,
 * uses left, not already used by this player), then creates the grant and both redemption records. Every failure
 * except "you already used it" gives the same answer, and attempts are limited per player per hour.
 */
export const redeemCode = playerCallable('redeemCode', async (data, caller) => {
  const code = normalizeCode(typeof data.code === 'string' ? data.code : '');
  await consumeAttempt(`redeem_${caller.uid}`, REDEEM_ATTEMPTS_PER_HOUR.value(), HOUR);
  if (!isValidCode(code)) fail('not-found', 'invalid-code', INVALID);

  const codeRef = db.collection('codes').doc(code);
  const mineRef = userRef(caller.uid).collection('redemptions').doc(code);
  const grantId = `redeem_${code}`;

  const result = await db.runTransaction(async (tx) => {
    const [codeSnap, userSnap, mineSnap] = await tx.getAll(codeRef, userRef(caller.uid), mineRef);
    assertNotBanned(userSnap.data());

    const doc = codeSnap.exists ? (codeSnap.data() as CodeDoc<Timestamp>) : null;
    const verdict = evaluateCode(
      doc && {
        active: doc.active,
        startsAt: millis(doc.startsAt),
        expiresAt: millis(doc.expiresAt),
        maxUses: doc.maxUses,
        uses: doc.uses ?? 0,
      },
      Date.now(),
      mineSnap.exists,
    );
    if (verdict === 'already-redeemed') fail('already-exists', 'already-redeemed', 'You already redeemed this code.');
    if (verdict !== 'ok' || !doc) fail('not-found', 'invalid-code', INVALID);

    const now = Timestamp.now();
    const message = doc.message || `Code ${code} redeemed.`;
    tx.create(grantRef(caller.uid, grantId), newGrant('redeem', doc.rewards, 'Code redeemed', message, { code }));
    tx.create(mineRef, { code, rewards: doc.rewards, grantId, at: now } satisfies MyRedemptionDoc<Timestamp>);
    tx.create(codeRef.collection('redemptions').doc(caller.uid), { uid: caller.uid, at: now } satisfies CodeRedemptionDoc<Timestamp>);
    // Limited codes count uses inside the transaction (the limit must hold). Open codes count afterwards, so a
    // popular launch code doesn't make every redemption contend on one document.
    if (doc.maxUses !== null) tx.update(codeRef, { uses: FieldValue.increment(1) });
    bumpStats(tx, { redemptions: 1 });
    return { code, rewards: doc.rewards, message, open: doc.maxUses === null };
  });

  if (result.open) {
    await codeRef.update({ uses: FieldValue.increment(1) }).catch((error) => logger.warn('code use count failed', { code, error: String(error) }));
  }
  return { code: result.code, rewards: result.rewards, message: result.message };
});
