import type { MailDoc } from '../shared/types.js';
import { playerCallable } from '../lib/callable.js';
import { FieldValue, Timestamp, adminAuth, db } from '../lib/firebase.js';
import { grantRef, newGrant } from '../lib/records.js';
import { isBanned, userRef } from '../lib/players.js';

/** Grant id for a global mail: one per player per mail, so collecting twice can never double it. */
export function mailGrantId(mailId: string): string {
  return `mail_${mailId}`;
}

/**
 * Gift to everyone: instead of writing a grant for every player, staff publish one mail/{id}; each player's game
 * calls this on sign-in and resume, and the server turns every active mail they haven't had yet into a grant.
 */
export const collectMail = playerCallable('collectMail', async (_data, caller) => {
  const now = Date.now();
  const active = await db.collection('mail').where('active', '==', true).get();
  const mails = active.docs.filter((doc) => {
    const mail = doc.data() as MailDoc<Timestamp>;
    return mail.startsAt.toMillis() <= now && mail.expiresAt.toMillis() > now;
  });
  if (mails.length === 0) return { created: 0 };
  if (isBanned((await userRef(caller.uid).get()).data())) return { created: 0 };

  const accountCreated = Date.parse((await adminAuth.getUser(caller.uid)).metadata.creationTime) || now;
  let created = 0;
  for (const doc of mails) {
    const mail = doc.data() as MailDoc<Timestamp>;
    if (!mail.newPlayersToo && accountCreated > mail.createdAt.toMillis()) continue;
    const ref = grantRef(caller.uid, mailGrantId(doc.id));
    const isNew = await db.runTransaction(async (tx) => {
      if ((await tx.get(ref)).exists) return false;
      tx.create(ref, newGrant(mail.kind === 'compensation' ? 'compensation' : 'gift', mail.rewards, mail.title, mail.message, { mailId: doc.id }));
      return true;
    });
    if (!isNew) continue;
    created++;
    // Outside the transaction: every player collects the same mail, so the counter mustn't be contended.
    await doc.ref.update({ collected: FieldValue.increment(1) }).catch(() => undefined);
  }
  return { created };
});
