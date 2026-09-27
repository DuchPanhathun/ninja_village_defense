import type { UserRecord } from 'firebase-admin/auth';
import type { DocumentData, DocumentSnapshot } from 'firebase-admin/firestore';
import type { GrantView, OrderView, PlayerDetail, PlayerSummary } from '../shared/api.js';
import { validateRewards } from '../shared/rewards.js';
import type { GrantDoc, ModerationLogDoc, OrderDoc } from '../shared/types.js';
import { bool, fail, int, staffCallable, str, uidArg, type StaffCaller } from '../lib/callable.js';
import { Timestamp, adminAuth, db, millis } from '../lib/firebase.js';
import { cleanDisplayName, isBanned, readSaveStore, userRef } from '../lib/players.js';
import { audit, bumpStats, grantRef, newGrant } from '../lib/records.js';

const NAME_MIN = 3;
const NAME_MAX = 16;

function num(value: unknown): number {
  return typeof value === 'number' && Number.isFinite(value) ? value : 0;
}

async function authUser(uid: string): Promise<UserRecord | null> {
  try {
    return await adminAuth.getUser(uid);
  } catch {
    return null;
  }
}

function lastActive(user: DocumentData | undefined, record: UserRecord | undefined): number | null {
  const times = [millis(user?.updatedAt), record?.metadata.lastRefreshTime ? Date.parse(record.metadata.lastRefreshTime) : null]
    .filter((t): t is number => typeof t === 'number' && !Number.isNaN(t));
  return times.length ? Math.max(...times) : null;
}

/** Dashboard counts, with Firestore count() aggregations (about one read per 1,000 players). */
export const adminCounts = staffCallable('adminCounts', 'read', async () => {
  const users = db.collection('users');
  const since = (days: number) => Timestamp.fromMillis(Date.now() - days * 86_400_000);
  const [players, activeDay, activeWeek, activeMonth, banned] = await Promise.all([
    users.count().get(),
    users.where('updatedAt', '>=', since(1)).count().get(),
    users.where('updatedAt', '>=', since(7)).count().get(),
    users.where('updatedAt', '>=', since(30)).count().get(),
    users.where('banned', '==', true).count().get(),
  ]);
  return {
    players: players.data().count,
    activeDay: activeDay.data().count,
    activeWeek: activeWeek.data().count,
    activeMonth: activeMonth.data().count,
    banned: banned.data().count,
  };
});

/** Find players by account ID, email or the start of their display name (case-sensitive, like the game shows it). */
export const adminSearchPlayers = staffCallable('adminSearchPlayers', 'read', async (data) => {
  const query = str(data.query, 'Search', { min: 1, max: 128 });
  const uids = new Set<string>();

  if (query.includes('@')) {
    for (const email of new Set([query, query.toLowerCase()])) {
      const record = await adminAuth.getUserByEmail(email).catch(() => null);
      if (record) uids.add(record.uid);
    }
  } else {
    if (!query.includes('/') && !query.includes(' ') && (await authUser(query))) uids.add(query);
    const byName = await db.collection('users').where('displayName', '>=', query).where('displayName', '<', `${query}`).limit(20).get();
    byName.docs.forEach((doc) => uids.add(doc.id));
  }
  if (uids.size === 0) return { players: [] };

  const ids = [...uids].slice(0, 25);
  const [docs, records] = await Promise.all([
    db.getAll(...ids.map((uid) => userRef(uid))),
    adminAuth.getUsers(ids.map((uid) => ({ uid }))),
  ]);
  const byUid = new Map(records.users.map((record) => [record.uid, record]));
  const players: PlayerSummary[] = docs.map((doc) => {
    const user = doc.data();
    const record = byUid.get(doc.id);
    return {
      uid: doc.id,
      displayName: typeof user?.displayName === 'string' ? user.displayName : '(no profile yet)',
      email: record?.email ?? null,
      anonymous: !!record && record.providerData.length === 0,
      highestWave: num(user?.highestWave),
      lastActive: lastActive(user, record),
      banned: isBanned(user),
    };
  });
  return { players };
});

function grantView(doc: DocumentSnapshot): GrantView {
  const g = doc.data() as GrantDoc<Timestamp>;
  return {
    id: doc.id,
    kind: g.kind,
    title: g.title,
    rewards: g.rewards,
    createdAt: millis(g.createdAt) ?? 0,
    claimedAt: millis(g.claimedAt),
    revokedAt: millis(g.revokedAt),
  };
}

export function orderView(doc: DocumentSnapshot): OrderView {
  const o = doc.data() as OrderDoc<Timestamp>;
  return {
    id: doc.id,
    productName: o.productName,
    amount: o.amount,
    currency: o.currency,
    status: o.status,
    provider: o.provider,
    createdAt: millis(o.createdAt) ?? 0,
    paidAt: millis(o.paidAt),
  };
}

/** Everything support needs on one page. The cloud save itself stays on the server (it can be ~1 MB). */
export const adminGetPlayer = staffCallable('adminGetPlayer', 'read', async (data) => {
  const uid = uidArg(data.uid);
  const user = userRef(uid);
  const [record, userSnap, villageSnap, boardSnap, deletionSnap, grants, redemptions, orders, log] = await Promise.all([
    authUser(uid),
    user.get(),
    db.collection('villages').doc(uid).get(),
    db.collection('leaderboard').doc(uid).get(),
    db.collection('deletionRequests').doc(uid).get(),
    user.collection('grants').orderBy('createdAt', 'desc').limit(50).get(),
    user.collection('redemptions').orderBy('at', 'desc').limit(50).get(),
    db.collection('orders').where('uid', '==', uid).orderBy('createdAt', 'desc').limit(50).get(),
    user.collection('moderationLog').orderBy('at', 'desc').limit(50).get(),
  ]);
  if (!record && !userSnap.exists) fail('not-found', 'no-player', 'No player with this ID.');

  const u = userSnap.data();
  const v = villageSnap.data();
  const b = boardSnap.data();
  const d = deletionSnap.data();
  const detail: PlayerDetail = {
    uid,
    auth: record && {
      email: record.email ?? null,
      emailVerified: record.emailVerified,
      anonymous: record.providerData.length === 0,
      providers: record.providerData.map((p) => p.providerId),
      createdAt: Date.parse(record.metadata.creationTime) || null,
      lastSignInAt: lastActive(undefined, record),
      disabled: record.disabled,
    },
    profile: u
      ? {
          displayName: typeof u.displayName === 'string' ? u.displayName : '',
          totalRuns: num(u.totalRuns),
          highestWave: num(u.highestWave),
          buildingLevels: num(u.buildingLevels),
          coins: num(u.coins),
          gems: num(u.gems),
          updatedAt: millis(u.updatedAt),
        }
      : null,
    village: v
      ? {
          castleLevel: num(v.castleLevel),
          highestWave: num(v.highestWave),
          chaptersCleared: num(v.chaptersCleared),
          achievementTiers: num(v.achievementTiers),
          likes: num(v.likes),
          updatedAt: millis(v.updatedAt),
        }
      : null,
    leaderboard: b ? { bestWave: num(b.bestWave), bestKills: num(b.bestKills) } : null,
    appStore: readSaveStore(u?.save),
    moderation: {
      banned: isBanned(u),
      bannedUntil: millis(u?.bannedUntil),
      banReason: typeof u?.banReason === 'string' ? u.banReason : '',
      nameOverride: typeof u?.nameOverride === 'string' ? u.nameOverride : null,
      leaderboardHidden: u?.leaderboardHidden === true,
      villageHidden: u?.villageHidden === true,
      reviewFlag: u?.reviewFlag
        ? { reason: String(u.reviewFlag.reason ?? ''), orderId: u.reviewFlag.orderId ?? null, at: millis(u.reviewFlag.at) ?? 0 }
        : null,
    },
    grants: grants.docs.map(grantView),
    redemptions: redemptions.docs.map((doc) => ({ code: doc.id, rewards: doc.data().rewards ?? [], at: millis(doc.data().at) ?? 0 })),
    orders: orders.docs.map(orderView),
    moderationLog: log.docs.map((doc) => {
      const entry = doc.data() as ModerationLogDoc<Timestamp>;
      return { at: millis(entry.at) ?? 0, by: entry.by, action: entry.action, reason: entry.reason, until: millis(entry.until) };
    }),
    deletion: d ? { status: d.status, createdAt: millis(d.createdAt) ?? 0, note: d.note ?? '' } : null,
  };
  return detail;
});

async function requireAccount(uid: string): Promise<void> {
  if (!(await authUser(uid))) fail('not-found', 'no-player', 'No account with this ID.');
}

function logEntry(staff: StaffCaller, action: ModerationLogDoc['action'], reason: string, until: Timestamp | null = null): ModerationLogDoc<Timestamp> {
  return { at: Timestamp.now(), by: staff.email ?? staff.uid, action, reason, until };
}

export const adminSendGift = staffCallable('adminSendGift', 'players.gift', async (data, staff) => {
  const uid = uidArg(data.uid);
  const rewards = validateRewards(data.rewards);
  const title = str(data.title, 'Title', { min: 1, max: 60 });
  const message = str(data.message ?? '', 'Message', { max: 300 });
  await requireAccount(uid);

  const ref = grantRef(uid, `gift_${db.collection('_').doc().id}`);
  const batch = db.batch();
  batch.create(ref, newGrant('gift', rewards, title, message, { by: staff.email ?? staff.uid }));
  bumpStats(batch, { gifts: 1 });
  audit(batch, staff, 'gift.send', uid, { grantId: ref.id, rewards, title });
  await batch.commit();
  return { grantId: ref.id };
});

/**
 * Ban or unban. A ban hides the player's leaderboard entry and public village at once; the Firestore rules stop
 * them publishing again until the ban ends. The game reads users/{uid}.banned and shows a message (WEB 1).
 */
export const adminSetBan = staffCallable('adminSetBan', 'players.ban', async (data, staff) => {
  const uid = uidArg(data.uid);
  const banned = bool(data.banned, 'Banned');
  const reason = str(data.reason ?? '', 'Reason', { min: banned ? 3 : 0, max: 200 });
  const days = data.days === null || data.days === undefined ? null : int(data.days, 'Days', { min: 1, max: 3650 });
  await requireAccount(uid);

  const until = banned && days !== null ? Timestamp.fromMillis(Date.now() + days * 86_400_000) : null;
  const batch = db.batch();
  batch.set(userRef(uid), { banned, bannedUntil: until, banReason: banned ? reason : '' }, { merge: true });
  if (banned) {
    batch.delete(db.collection('leaderboard').doc(uid));
    batch.delete(db.collection('villages').doc(uid));
  }
  batch.set(userRef(uid).collection('moderationLog').doc(), logEntry(staff, banned ? 'ban' : 'unban', reason, until));
  audit(batch, staff, banned ? 'player.ban' : 'player.unban', uid, { reason, days });
  await batch.commit();
  return { ok: true };
});

/**
 * Replaces a bad display name everywhere it's public and locks it (`nameOverride`): the rules only accept this
 * name on the leaderboard and village until staff clear the lock (an empty name clears it).
 */
export const adminRenamePlayer = staffCallable('adminRenamePlayer', 'players.rename', async (data, staff) => {
  const uid = uidArg(data.uid);
  const raw = cleanDisplayName(typeof data.name === 'string' ? data.name : '');
  await requireAccount(uid);

  const batch = db.batch();
  if (raw === '') {
    batch.set(userRef(uid), { nameOverride: null }, { merge: true });
    audit(batch, staff, 'player.rename.unlock', uid, {});
  } else {
    const name = str(raw, 'Name', { min: NAME_MIN, max: NAME_MAX });
    batch.set(userRef(uid), { displayName: name, nameOverride: name }, { merge: true });
    const [board, village] = await db.getAll(db.collection('leaderboard').doc(uid), db.collection('villages').doc(uid));
    if (board.exists) batch.update(board.ref, { displayName: name });
    if (village.exists) batch.update(village.ref, { displayName: name });
    batch.set(userRef(uid).collection('moderationLog').doc(), logEntry(staff, 'rename', name));
    audit(batch, staff, 'player.rename', uid, { name });
  }
  await batch.commit();
  return { ok: true };
});

/** Hide or show a player's leaderboard entry / public village without banning them. */
export const adminModerate = staffCallable('adminModerate', 'players.moderate', async (data, staff) => {
  const uid = uidArg(data.uid);
  const reason = str(data.reason ?? '', 'Reason', { max: 200 });
  const update: Record<string, boolean> = {};
  if (data.leaderboardHidden !== undefined) update.leaderboardHidden = bool(data.leaderboardHidden, 'Leaderboard');
  if (data.villageHidden !== undefined) update.villageHidden = bool(data.villageHidden, 'Village');
  if (Object.keys(update).length === 0) fail('invalid-argument', 'bad-input', 'Nothing to change.');
  await requireAccount(uid);

  const batch = db.batch();
  batch.set(userRef(uid), update, { merge: true });
  const log = userRef(uid).collection('moderationLog');
  if (update.leaderboardHidden !== undefined) {
    if (update.leaderboardHidden) batch.delete(db.collection('leaderboard').doc(uid));
    batch.set(log.doc(), logEntry(staff, update.leaderboardHidden ? 'hide-leaderboard' : 'show-leaderboard', reason));
  }
  if (update.villageHidden !== undefined) {
    if (update.villageHidden) batch.delete(db.collection('villages').doc(uid));
    batch.set(log.doc(), logEntry(staff, update.villageHidden ? 'hide-village' : 'show-village', reason));
  }
  audit(batch, staff, 'player.moderate', uid, { ...update, reason });
  await batch.commit();
  return { ok: true };
});

export const adminClearReview = staffCallable('adminClearReview', 'players.moderate', async (data, staff) => {
  const uid = uidArg(data.uid);
  const batch = db.batch();
  batch.set(userRef(uid), { reviewFlag: null }, { merge: true });
  audit(batch, staff, 'player.review.clear', uid, {});
  await batch.commit();
  return { ok: true };
});
