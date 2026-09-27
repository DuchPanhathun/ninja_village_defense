// Fills the local emulators with demo data so the website and admin site have something to show.
// Run from firebase/ while the emulators are up:
//   npm --prefix functions run seed
// Accounts (password for all: password123):
//   player@example.com  — a player with progress, grants, a redeemed code and orders
//   support@example.com — support role
//   admin@example.com   — admin role with an SMS second factor (+1 555-555-0100; the emulator shows the code)
import { initializeApp } from 'firebase-admin/app';
import { getAuth } from 'firebase-admin/auth';
import { Timestamp, getFirestore } from 'firebase-admin/firestore';

process.env.FIRESTORE_EMULATOR_HOST ??= '127.0.0.1:8080';
process.env.FIREBASE_AUTH_EMULATOR_HOST ??= '127.0.0.1:9099';
const projectId = process.env.GCLOUD_PROJECT ?? 'demo-ninja';
if (!projectId.startsWith('demo-')) throw new Error('Refusing to seed a non-demo project.');

initializeApp({ projectId });
const auth = getAuth();
const db = getFirestore();
const DAY = 86_400_000;
const now = Date.now();
const ts = (ms) => Timestamp.fromMillis(ms);
const PASSWORD = 'password123';

async function user(props, claims) {
  const existing = await auth.getUser(props.uid).catch(() => null);
  if (!existing) await auth.createUser({ password: PASSWORD, emailVerified: true, ...props });
  if (claims) await auth.setCustomUserClaims(props.uid, claims);
}

const names = ['Aki', 'ShadowFox', 'Hanzo', 'Mei', 'KunaiKid', 'Sakura', 'Ronin77', 'Yuki', 'OniSlayer', 'Kaze', 'Tora', 'Hikari'];

async function main() {
  await user({ uid: 'player-aki', email: 'player@example.com', displayName: 'Aki' });
  await user({ uid: 'staff-support', email: 'support@example.com' }, { role: 'support' });
  await user({
    uid: 'staff-admin',
    email: 'admin@example.com',
    multiFactor: { enrolledFactors: [{ phoneNumber: '+15555550100', factorId: 'phone', displayName: 'Test phone' }] },
  }, { role: 'admin' });
  for (let i = 1; i < names.length; i++) await user({ uid: `player-${i}` });

  const batch = db.batch();
  const set = (path, data) => batch.set(db.doc(path), data);

  // Players, public villages and the leaderboard.
  names.forEach((name, i) => {
    const uid = i === 0 ? 'player-aki' : `player-${i}`;
    const wave = [42, 97, 88, 61, 55, 49, 40, 33, 29, 25, 18, 12][i];
    set(`users/${uid}`, {
      displayName: name, totalRuns: wave * 2 + 7, highestWave: wave, buildingLevels: 10 + i, coins: 12_000 - i * 700, gems: 340 - i * 20,
      lastSavedTicks: 0, version: 2, updatedAt: ts(now - i * 5 * 3_600_000),
      save: JSON.stringify({ Version: 2, Store: { AdsRemoved: i === 1, PurchasedProductIds: i === 1 ? ['com.thun.ninjavillagedefense.remove_ads'] : [], StarterPackPurchased: i === 2, ProcessedTransactionIds: i < 3 ? ['GPA.1', 'GPA.2'] : [], PremiumPassSeasons: [] } }),
    });
    set(`leaderboard/${uid}`, { displayName: name, bestWave: wave, bestKills: wave * 31 + i * 7, updatedAt: ts(now - i * 3_600_000) });
    set(`villages/${uid}`, {
      displayName: name, castleLevel: Math.max(1, Math.round(wave / 10)), highestWave: wave, chaptersCleared: Math.min(5, Math.floor(wave / 15)),
      achievementTiers: 3 + i, snapshot: '{}', likes: 20 - i, likesWeek: 1, rankKey: 1_000_000 + (20 - i), updatedAt: ts(now - i * 7_200_000),
    });
  });

  // Codes: an open launch code, a campaign code and a small bulk batch.
  const code = (id, extra) => set(`codes/${id}`, {
    code: id, kind: 'open', rewards: [{ type: 'gems', id: 'gems', amount: 100 }], message: '', maxUses: null, uses: 0, active: true,
    startsAt: null, expiresAt: ts(now + 30 * DAY), batchId: null, note: '', createdAt: ts(now - 2 * DAY), createdBy: 'admin@example.com', ...extra,
  });
  code('WELCOME2026', { message: 'Welcome to the village!', uses: 1, note: 'Launch' });
  code('STREAMNIGHT', { kind: 'campaign', maxUses: 500, uses: 213, rewards: [{ type: 'crate', id: 'silver', amount: 2 }], note: 'Twitch stream', createdAt: ts(now - DAY) });
  code('OLDEVENT', { active: false, expiresAt: ts(now - DAY), createdAt: ts(now - 20 * DAY) });
  ['YTK7M2QXPA4R9C', 'YTH3WN8CZE6TQ2', 'YTP9RD4VBJ2MX7'].forEach((id, i) => code(id, {
    kind: 'single', maxUses: 1, uses: i === 0 ? 1 : 0, batchId: 'batch_demo', rewards: [{ type: 'skin', id: 'skin_samurai_gold', amount: 1 }], note: 'YouTube giveaway',
  }));
  set('codes/WELCOME2026/redemptions/player-aki', { uid: 'player-aki', at: ts(now - DAY) });
  set('users/player-aki/redemptions/WELCOME2026', { code: 'WELCOME2026', rewards: [{ type: 'gems', id: 'gems', amount: 100 }], grantId: 'redeem_WELCOME2026', at: ts(now - DAY) });

  // Shop products (mirroring the app's catalog).
  const product = (id, data) => set(`products/${id}`, {
    description: '', salePrice: null, active: true, oncePerPlayer: false, appProductId: null, badge: '', currency: 'USD', updatedAt: ts(now), ...data,
  });
  product('starter_pack', { name: 'Starter Pack', description: '300 gems, 5,000 coins and the Beast Ninja hero — once only', image: 'store_starter_pack.png', price: 299, oncePerPlayer: true, appProductId: 'com.thun.ninjavillagedefense.starter_pack', badge: 'Once only', sortOrder: 1,
    rewards: [{ type: 'gems', id: 'gems', amount: 300 }, { type: 'coins', id: 'coins', amount: 5000 }, { type: 'hero', id: 'beast_ninja', amount: 1 }, { type: 'entitlement', id: 'starter_pack', amount: 1 }] });
  product('gems_100', { name: 'Pouch of Gems', description: '100 gems', image: 'store_gems_100.png', price: 99, sortOrder: 10, rewards: [{ type: 'gems', id: 'gems', amount: 100 }] });
  product('gems_550', { name: 'Chest of Gems', description: '550 gems (+10% bonus)', image: 'store_gems_550.png', price: 499, salePrice: 399, badge: 'Sale', sortOrder: 20, rewards: [{ type: 'gems', id: 'gems', amount: 550 }] });
  product('gems_1200', { name: 'Vault of Gems', description: '1,200 gems (+20% bonus)', image: 'store_gems_1200.png', price: 999, badge: 'Best value', sortOrder: 30, rewards: [{ type: 'gems', id: 'gems', amount: 1200 }] });
  product('sakura_bundle', { name: 'Sakura Bundle', description: 'The Sakura Assassin skin and a Surprise Box', image: 'store_offer_sakura_bundle.png', price: 699, sortOrder: 40, active: false,
    rewards: [{ type: 'skin', id: 'skin_assassin_sakura', amount: 1 }, { type: 'crate', id: 'surprise', amount: 1 }] });

  // Aki's inbox and orders.
  const grant = (id, data) => set(`users/player-aki/grants/${id}`, { revokedAt: null, message: '', source: {}, ...data });
  grant('redeem_WELCOME2026', { kind: 'redeem', title: 'Code redeemed', rewards: [{ type: 'gems', id: 'gems', amount: 100 }], createdAt: ts(now - DAY), claimedAt: ts(now - DAY + 600_000), source: { code: 'WELCOME2026' } });
  grant('purchase_sandbox_demo1', { kind: 'purchase', title: 'Thanks for your purchase!', rewards: [{ type: 'gems', id: 'gems', amount: 1200 }], createdAt: ts(now - 3 * 3_600_000), claimedAt: null, source: { orderId: 'demo-order-1' } });
  grant('gift_demo', { kind: 'gift', title: 'Sorry about the crash!', rewards: [{ type: 'crate', id: 'silver', amount: 1 }], createdAt: ts(now - 2 * DAY), claimedAt: ts(now - DAY), source: { by: 'support@example.com' } });

  const order = (id, data) => set(`orders/${id}`, {
    email: null, provider: 'sandbox', providerRef: null, paymentId: `sbx_${id}`, grantId: null, paidAt: null, refundedAt: null, note: '', currency: 'USD', ...data,
  });
  order('demo-order-1', { uid: 'player-aki', email: 'player@example.com', productId: 'gems_1200', productName: 'Vault of Gems', rewards: [{ type: 'gems', id: 'gems', amount: 1200 }], amount: 999, status: 'paid', createdAt: ts(now - 3 * 3_600_000), paidAt: ts(now - 3 * 3_600_000), grantId: 'purchase_sandbox_demo1' });
  order('demo-order-2', { uid: 'player-1', productId: 'gems_550', productName: 'Chest of Gems', rewards: [{ type: 'gems', id: 'gems', amount: 550 }], amount: 399, status: 'paid', createdAt: ts(now - DAY), paidAt: ts(now - DAY) });
  order('demo-order-3', { uid: 'player-2', productId: 'gems_100', productName: 'Pouch of Gems', rewards: [{ type: 'gems', id: 'gems', amount: 100 }], amount: 99, status: 'review', note: 'Paid 1 USD, expected 99 USD.', createdAt: ts(now - 2 * DAY) });
  order('demo-order-4', { uid: 'player-3', productId: 'gems_550', productName: 'Chest of Gems', rewards: [{ type: 'gems', id: 'gems', amount: 550 }], amount: 399, status: 'refunded', note: 'Refunded before the rewards were collected: grant revoked.', createdAt: ts(now - 4 * DAY), paidAt: ts(now - 4 * DAY), refundedAt: ts(now - 3 * DAY) });

  // Dashboard history: 30 days of counters.
  for (let d = 29; d >= 0; d--) {
    const key = new Date(now - d * DAY).toISOString().slice(0, 10);
    const wave = Math.sin((30 - d) / 4) * 0.3 + 1 + (30 - d) / 40;
    const orders = Math.round(4 * wave + (d % 7 === 1 ? 5 : 0));
    set(`stats/${key}`, { newPlayers: Math.round(38 * wave + (d % 7 === 1 ? 25 : 0)), orders, refunds: d % 9 === 0 ? 1 : 0, redemptions: Math.round(15 * wave), gifts: d % 5 === 0 ? 2 : 0, revenue: { USD: orders * 449 } });
  }

  // Global mail, a deletion request, staff list and a few audit entries.
  set('mail/demo-downtime', { kind: 'compensation', title: 'Sorry for the downtime!', message: 'The servers were down for an hour on Friday.', rewards: [{ type: 'gems', id: 'gems', amount: 200 }], active: true, startsAt: ts(now - DAY), expiresAt: ts(now + 13 * DAY), newPlayersToo: false, createdAt: ts(now - DAY), createdBy: 'admin@example.com', collected: 1843 });
  set('deletionRequests/player-9', { uid: 'player-9', email: null, displayName: 'OniSlayer', source: 'website', status: 'pending', createdAt: ts(now - 3 * DAY), processedAt: null, processedBy: null, note: '' });
  set('staff/staff-admin', { email: 'admin@example.com', role: 'admin', updatedAt: ts(now - 10 * DAY), updatedBy: 'set-role script' });
  set('staff/staff-support', { email: 'support@example.com', role: 'support', updatedAt: ts(now - 9 * DAY), updatedBy: 'admin@example.com' });
  const audit = (ago, action, targetUid, details, actor = 'support@example.com') => batch.set(db.collection('audit').doc(), {
    at: ts(now - ago), actorUid: actor === 'admin@example.com' ? 'staff-admin' : 'staff-support', actorEmail: actor, role: actor === 'admin@example.com' ? 'admin' : 'support', action, targetUid, details,
  });
  audit(2 * DAY, 'gift.send', 'player-aki', { title: 'Sorry about the crash!', rewards: [{ type: 'crate', id: 'silver', amount: 1 }] });
  audit(DAY, 'code.create', null, { code: 'STREAMNIGHT', kind: 'campaign', maxUses: 500 }, 'admin@example.com');
  audit(DAY - 3_600_000, 'mail.send', null, { id: 'demo-downtime', title: 'Sorry for the downtime!' }, 'admin@example.com');

  await batch.commit();
  console.log('Seeded demo data. Sign in with player@example.com / support@example.com / admin@example.com (password123).');
}

main().then(() => process.exit(0), (error) => {
  console.error(error);
  process.exit(1);
});
