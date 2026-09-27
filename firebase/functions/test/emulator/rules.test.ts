/**
 * Firestore rules tests (task_web.text WEB 7). Run from firebase/:
 *   firebase emulators:exec --project demo-ninja --only firestore "npm --prefix functions run test:rules"
 */
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { afterAll, beforeAll, beforeEach, describe, it } from 'vitest';
import {
  assertFails,
  assertSucceeds,
  initializeTestEnvironment,
  type RulesTestEnvironment,
} from '@firebase/rules-unit-testing';
import {
  Timestamp,
  collection,
  deleteDoc,
  doc,
  getDoc,
  getDocs,
  query,
  serverTimestamp,
  setDoc,
  updateDoc,
  where,
} from 'firebase/firestore';

let env: RulesTestEnvironment;

beforeAll(async () => {
  const [host, port] = (process.env.FIRESTORE_EMULATOR_HOST ?? '127.0.0.1:8080').split(':');
  env = await initializeTestEnvironment({
    projectId: 'demo-ninja',
    firestore: { rules: readFileSync(resolve(__dirname, '../../../firestore.rules'), 'utf8'), host, port: Number(port) },
  });
});
afterAll(() => env?.cleanup());
beforeEach(() => env.clearFirestore());

const alice = () => env.authenticatedContext('alice').firestore();
const bob = () => env.authenticatedContext('bob').firestore();
const guest = () => env.unauthenticatedContext().firestore();
const staff = (role: string, mfa = false) =>
  env.authenticatedContext(`staff_${role}`, { role, ...(mfa ? { firebase: { sign_in_second_factor: 'totp' } } : {}) }).firestore();

/** Writes as the server (rules bypassed), like the Cloud Functions' Admin SDK. */
async function seed(path: string, data: Record<string, unknown>) {
  await env.withSecurityRulesDisabled(async (ctx) => {
    await setDoc(doc(ctx.firestore(), path), data);
  });
}

const leaderboardEntry = (name = 'Alice') => ({ displayName: name, bestWave: 12, bestKills: 300, updatedAt: serverTimestamp() });
const village = (name = 'Alice') => ({
  displayName: name, castleLevel: 3, highestWave: 12, chaptersCleared: 1, achievementTiers: 2, snapshot: '{}',
  likes: 0, likesWeek: 1, rankKey: 1_000_000, updatedAt: serverTimestamp(),
});

describe('users/{uid} — the game save still works, moderation fields are server-only', () => {
  it('lets the owner write the save and progress fields (as the game does)', async () => {
    await assertSucceeds(setDoc(doc(alice(), 'users/alice'), { save: '{}', lastSavedTicks: 1, version: 2, coins: 5, gems: 1, updatedAt: serverTimestamp() }, { merge: true }));
    await assertSucceeds(setDoc(doc(alice(), 'users/alice'), { displayName: 'Alice', totalRuns: 3, highestWave: 9 }, { merge: true }));
    await assertSucceeds(getDoc(doc(alice(), 'users/alice')));
    await assertFails(getDoc(doc(bob(), 'users/alice')));
  });

  it('keeps the owner from setting or clearing moderation fields', async () => {
    await assertFails(setDoc(doc(alice(), 'users/alice'), { save: '{}', banned: false }));
    await seed('users/alice', { save: '{}', banned: true, banReason: 'cheating' });
    await assertFails(updateDoc(doc(alice(), 'users/alice'), { banned: false }));
    await assertFails(setDoc(doc(alice(), 'users/alice'), { save: '{}' }));
    await assertFails(updateDoc(doc(alice(), 'users/alice'), { nameOverride: null }));
    await assertFails(updateDoc(doc(alice(), 'users/alice'), { reviewFlag: null }));
    // ...but the normal merge upload leaves them untouched and is allowed.
    await assertSucceeds(setDoc(doc(alice(), 'users/alice'), { save: '{"x":1}', gems: 10 }, { merge: true }));
  });

  it('still rejects oversized saves', async () => {
    await assertFails(setDoc(doc(alice(), 'users/alice'), { save: 'x'.repeat(900_001) }));
  });
});

describe('grants — read your own, only mark claimed once', () => {
  const grant = { kind: 'gift', rewards: [{ type: 'gems', id: 'gems', amount: 50 }], title: 'Hi', message: '', createdAt: Timestamp.now(), claimedAt: null, revokedAt: null, source: {} };

  it('lets only the owner read', async () => {
    await seed('users/alice/grants/g1', grant);
    await assertSucceeds(getDoc(doc(alice(), 'users/alice/grants/g1')));
    await assertSucceeds(getDocs(collection(alice(), 'users/alice/grants')));
    await assertFails(getDoc(doc(bob(), 'users/alice/grants/g1')));
  });

  it('allows claimedAt = server time once, nothing else', async () => {
    await seed('users/alice/grants/g1', grant);
    await assertFails(updateDoc(doc(alice(), 'users/alice/grants/g1'), { claimedAt: Timestamp.fromMillis(1) }));
    await assertFails(updateDoc(doc(alice(), 'users/alice/grants/g1'), { claimedAt: serverTimestamp(), rewards: [{ type: 'gems', id: 'gems', amount: 99999 }] }));
    await assertFails(updateDoc(doc(bob(), 'users/alice/grants/g1'), { claimedAt: serverTimestamp() }));
    await assertSucceeds(updateDoc(doc(alice(), 'users/alice/grants/g1'), { claimedAt: serverTimestamp() }));
    await assertFails(updateDoc(doc(alice(), 'users/alice/grants/g1'), { claimedAt: serverTimestamp() }));
  });

  it('never lets clients create, delete or claim revoked grants', async () => {
    await assertFails(setDoc(doc(alice(), 'users/alice/grants/mine'), grant));
    await seed('users/alice/grants/g1', grant);
    await assertFails(deleteDoc(doc(alice(), 'users/alice/grants/g1')));
    await seed('users/alice/grants/g2', { ...grant, revokedAt: Timestamp.now() });
    await assertFails(updateDoc(doc(alice(), 'users/alice/grants/g2'), { claimedAt: serverTimestamp() }));
  });

  it('keeps redemptions and moderation log read-only', async () => {
    await seed('users/alice/redemptions/CODE', { code: 'CODE' });
    await assertSucceeds(getDoc(doc(alice(), 'users/alice/redemptions/CODE')));
    await assertFails(setDoc(doc(alice(), 'users/alice/redemptions/FAKE'), { code: 'FAKE' }));
    await seed('users/alice/moderationLog/l1', { action: 'ban' });
    await assertFails(getDoc(doc(alice(), 'users/alice/moderationLog/l1')));
  });
});

describe('leaderboard and villages — banned, hidden and renamed players', () => {
  it('lets a normal player publish, as before', async () => {
    await assertSucceeds(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry()));
    await assertSucceeds(setDoc(doc(alice(), 'villages/alice'), village()));
    await seed('users/alice', { save: '{}' });
    await assertSucceeds(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry()));
    await assertFails(setDoc(doc(bob(), 'leaderboard/alice'), leaderboardEntry()));
  });

  it('blocks banned players until the ban ends', async () => {
    await seed('users/alice', { banned: true, bannedUntil: null });
    await assertFails(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry()));
    await assertFails(setDoc(doc(alice(), 'villages/alice'), village()));
    await seed('users/alice', { banned: true, bannedUntil: Timestamp.fromMillis(Date.now() + 3_600_000) });
    await assertFails(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry()));
    await seed('users/alice', { banned: true, bannedUntil: Timestamp.fromMillis(Date.now() - 1000) });
    await assertSucceeds(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry()));
  });

  it('keeps hidden content hidden', async () => {
    await seed('users/alice', { leaderboardHidden: true });
    await assertFails(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry()));
    await assertSucceeds(setDoc(doc(alice(), 'villages/alice'), village()));
    await seed('users/alice', { villageHidden: true });
    await assertFails(setDoc(doc(alice(), 'villages/alice'), village()));
  });

  it('only accepts the name staff set', async () => {
    await seed('users/alice', { nameOverride: 'Ninja123' });
    await assertFails(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry('BadName')));
    await assertSucceeds(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry('Ninja123')));
    await seed('users/alice', { nameOverride: null });
    await assertSucceeds(setDoc(doc(alice(), 'leaderboard/alice'), leaderboardEntry('Anything')));
  });

  it('stops banned visitors writing in visitors’ books', async () => {
    const visit = { name: 'Bob', visitDay: 1, likedWeek: 1, giftDay: 1, waterDay: 1, waterTicks: 0, at: serverTimestamp() };
    await assertSucceeds(setDoc(doc(bob(), 'villages/alice/visits/bob'), visit));
    await seed('users/bob', { banned: true });
    await assertFails(setDoc(doc(bob(), 'villages/alice/visits/bob'), visit));
  });

  it('no longer lets clients delete public entries', async () => {
    await seed('leaderboard/alice', { displayName: 'Alice', bestWave: 1, bestKills: 1 });
    await assertFails(deleteDoc(doc(alice(), 'leaderboard/alice')));
  });
});

describe('web platform collections', () => {
  it('shows active products to everyone, the rest to staff', async () => {
    await seed('products/gems_100', { name: 'Gems', active: true, sortOrder: 1 });
    await seed('products/secret', { name: 'Soon', active: false, sortOrder: 2 });
    await assertSucceeds(getDocs(query(collection(guest(), 'products'), where('active', '==', true))));
    await assertFails(getDocs(collection(guest(), 'products')));
    await assertFails(getDoc(doc(guest(), 'products/secret')));
    await assertSucceeds(getDoc(doc(staff('viewer'), 'products/secret')));
    await assertFails(setDoc(doc(staff('admin', true), 'products/new'), { name: 'x', active: true }));
  });

  it('lets buyers read only their own orders', async () => {
    await seed('orders/o1', { uid: 'alice', status: 'paid' });
    await assertSucceeds(getDoc(doc(alice(), 'orders/o1')));
    await assertSucceeds(getDocs(query(collection(alice(), 'orders'), where('uid', '==', 'alice'))));
    await assertFails(getDoc(doc(bob(), 'orders/o1')));
    await assertFails(getDocs(collection(alice(), 'orders')));
    await assertFails(updateDoc(doc(alice(), 'orders/o1'), { status: 'refunded' }));
    await assertFails(setDoc(doc(alice(), 'orders/o2'), { uid: 'alice', status: 'paid' }));
  });

  it('hides codes from players and guessers', async () => {
    await seed('codes/SAKURA2026', { active: true });
    await assertFails(getDoc(doc(alice(), 'codes/SAKURA2026')));
    await assertFails(getDocs(collection(alice(), 'codes')));
    await assertSucceeds(getDoc(doc(staff('viewer'), 'codes/SAKURA2026')));
    await assertFails(setDoc(doc(staff('admin', true), 'codes/NEW1'), { active: true }));
  });

  it('requires 2-step sign-in for admins, not for support and viewers', async () => {
    await seed('audit/a1', { action: 'x' });
    await seed('staff/staff_admin', { role: 'admin' });
    await assertFails(getDoc(doc(staff('admin'), 'audit/a1')));
    await assertSucceeds(getDoc(doc(staff('admin', true), 'audit/a1')));
    await assertSucceeds(getDoc(doc(staff('support'), 'audit/a1')));
    await assertFails(getDoc(doc(alice(), 'audit/a1')));
    await assertFails(getDoc(doc(env.authenticatedContext('x', { role: 'superuser' }).firestore(), 'audit/a1')));
    await assertFails(setDoc(doc(staff('admin', true), 'audit/a2'), { action: 'forged' }));
    await assertFails(deleteDoc(doc(staff('admin', true), 'audit/a1')));
    await assertSucceeds(getDocs(collection(staff('admin', true), 'staff')));
    await assertFails(getDocs(collection(staff('support'), 'staff')));
  });

  it('lets players see their own deletion request only', async () => {
    await seed('deletionRequests/alice', { uid: 'alice', status: 'pending' });
    await assertSucceeds(getDoc(doc(alice(), 'deletionRequests/alice')));
    await assertFails(getDoc(doc(bob(), 'deletionRequests/alice')));
    await assertFails(setDoc(doc(alice(), 'deletionRequests/alice'), { status: 'done' }));
    await assertSucceeds(getDoc(doc(staff('support'), 'deletionRequests/alice')));
  });

  it('denies everything else', async () => {
    await seed('rateLimits/redeem_alice', { count: 99 });
    await assertFails(getDoc(doc(alice(), 'rateLimits/redeem_alice')));
    await assertFails(setDoc(doc(alice(), 'rateLimits/redeem_alice'), { count: 0 }));
    await assertFails(getDoc(doc(alice(), 'mail/m1')));
    await assertFails(getDoc(doc(alice(), 'stats/2026-09-27')));
  });
});
