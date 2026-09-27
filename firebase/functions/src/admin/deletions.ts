import { logger } from 'firebase-functions/v2';
import type { DeletionRequestDoc } from '../shared/types.js';
import { staffCallable, str, uidArg } from '../lib/callable.js';
import { Timestamp, adminAuth, db } from '../lib/firebase.js';
import { userRef } from '../lib/players.js';
import { audit } from '../lib/records.js';

/**
 * Deletes everything about a player: the Auth account, users/{uid} with its grants, redemptions and log, the
 * public village and its visitors' book, the leaderboard entry and the code-redemption records. Orders stay (the
 * law requires payment records) but lose the email. Works with or without a request from the website (support
 * can process an emailed request by account ID).
 */
export async function deletePlayerData(uid: string): Promise<void> {
  const redemptions = await userRef(uid).collection('redemptions').get();
  const writer = db.bulkWriter();
  redemptions.docs.forEach((doc) => writer.delete(db.collection('codes').doc(doc.id).collection('redemptions').doc(uid)));
  writer.delete(db.collection('leaderboard').doc(uid));
  writer.delete(db.collection('server_time').doc(uid));
  writer.delete(db.collection('rateLimits').doc(`redeem_${uid}`));
  writer.delete(db.collection('rateLimits').doc(`checkout_${uid}`));
  const orders = await db.collection('orders').where('uid', '==', uid).get();
  orders.docs.forEach((doc) => writer.update(doc.ref, { email: null, accountDeleted: true }));
  await writer.close();

  await db.recursiveDelete(userRef(uid));
  await db.recursiveDelete(db.collection('villages').doc(uid));
  await adminAuth.deleteUser(uid).catch((error: { code?: string }) => {
    if (error.code !== 'auth/user-not-found') throw error;
  });
}

export const adminProcessDeletion = staffCallable('adminProcessDeletion', 'players.delete', async (data, staff) => {
  const uid = uidArg(data.uid);
  const note = str(data.note ?? '', 'Note', { max: 300 });
  const action = data.action === 'delete' ? 'delete' : 'reject';
  const ref = db.collection('deletionRequests').doc(uid);
  const existing = (await ref.get()).data() as DeletionRequestDoc<Timestamp> | undefined;

  if (action === 'delete') {
    const record = await adminAuth.getUser(uid).catch(() => null);
    await deletePlayerData(uid);
    logger.info('player deleted', { uid, by: staff.uid });
    const done: DeletionRequestDoc<Timestamp> = {
      uid,
      email: null,
      displayName: existing?.displayName ?? '',
      source: existing?.source ?? 'support',
      status: 'done',
      createdAt: existing?.createdAt ?? Timestamp.now(),
      processedAt: Timestamp.now(),
      processedBy: staff.email ?? staff.uid,
      note,
    };
    const batch = db.batch();
    batch.set(ref, done);
    audit(batch, staff, 'player.delete', uid, { hadEmail: !!record?.email, note });
    await batch.commit();
  } else {
    const batch = db.batch();
    batch.set(ref, { status: 'rejected', processedAt: Timestamp.now(), processedBy: staff.email ?? staff.uid, note }, { merge: true });
    audit(batch, staff, 'player.delete.reject', uid, { note });
    await batch.commit();
  }
  return { ok: true };
});
