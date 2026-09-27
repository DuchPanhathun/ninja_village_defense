import type { DeletionRequestDoc } from '../shared/types.js';
import { fail, playerCallable } from '../lib/callable.js';
import { Timestamp, db } from '../lib/firebase.js';
import { userRef } from '../lib/players.js';

function requestRef(uid: string) {
  return db.collection('deletionRequests').doc(uid);
}

/**
 * The signed-in player asks for their account and data to be deleted (Google Play's account-deletion
 * requirement). Staff process requests from the admin website; the player can cancel while it's pending.
 */
export const requestAccountDeletion = playerCallable('requestAccountDeletion', async (data, caller) => {
  if (data.confirm !== 'DELETE') fail('invalid-argument', 'confirm', 'Type DELETE to confirm.');
  const user = (await userRef(caller.uid).get()).data();
  return db.runTransaction(async (tx) => {
    const ref = requestRef(caller.uid);
    const existing = await tx.get(ref);
    if (existing.exists && existing.data()?.status === 'pending') return { status: 'pending' as const };
    const doc: DeletionRequestDoc<Timestamp> = {
      uid: caller.uid,
      email: caller.email,
      displayName: typeof user?.displayName === 'string' ? user.displayName : '',
      source: 'website',
      status: 'pending',
      createdAt: Timestamp.now(),
      processedAt: null,
      processedBy: null,
      note: '',
    };
    tx.set(ref, doc);
    return { status: 'pending' as const };
  });
});

export const cancelAccountDeletion = playerCallable('cancelAccountDeletion', async (_data, caller) => {
  return db.runTransaction(async (tx) => {
    const ref = requestRef(caller.uid);
    const snap = await tx.get(ref);
    if (!snap.exists || snap.data()?.status !== 'pending') fail('failed-precondition', 'not-pending', 'There is no pending request.');
    tx.update(ref, { status: 'canceled', processedAt: Timestamp.now(), processedBy: caller.uid });
    return { status: 'canceled' as const };
  });
});
