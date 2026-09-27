/**
 * End-to-end tests of the callable functions and the payment webhook on the emulators (task_web.text WEB 7).
 * Run from firebase/ after `npm --prefix functions run build`:
 *   firebase emulators:exec --project demo-ninja --only auth,firestore,functions "npm --prefix functions run test:functions"
 */
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { deleteApp, initializeApp as initClientApp, type FirebaseApp } from 'firebase/app';
import {
  connectAuthEmulator,
  createUserWithEmailAndPassword,
  getAuth,
  signInAnonymously,
  signOut,
  type Auth,
} from 'firebase/auth';
import { connectFunctionsEmulator, getFunctions, httpsCallable, type Functions } from 'firebase/functions';
import { initializeApp as initAdminApp } from 'firebase-admin/app';
import { getAuth as getAdminAuth } from 'firebase-admin/auth';
import { Timestamp, getFirestore as getAdminDb } from 'firebase-admin/firestore';
import type { Api, ApiName } from '../../src/shared/api.js';
import { signSandboxPayload } from '../../src/payments/providers.js';

const PROJECT = 'demo-ninja';
const REGION = 'us-central1';
const WEBHOOK_SECRET = 'emulator-webhook-secret';
const WEBHOOK_URL = `http://127.0.0.1:5001/${PROJECT}/${REGION}/paymentWebhook`;

initAdminApp({ projectId: PROJECT });
const db = getAdminDb();
const adminAuth = getAdminAuth();

/** One signed-in client per test player (separate app instances = separate sessions). */
class Player {
  app: FirebaseApp;
  auth: Auth;
  fns: Functions;
  uid = '';
  constructor(name: string) {
    this.app = initClientApp({ projectId: PROJECT, apiKey: 'fake-api-key' }, name);
    this.auth = getAuth(this.app);
    connectAuthEmulator(this.auth, 'http://127.0.0.1:9099', { disableWarnings: true });
    this.fns = getFunctions(this.app, REGION);
    connectFunctionsEmulator(this.fns, '127.0.0.1', 5001);
  }
  async signUp(email?: string) {
    const cred = email
      ? await createUserWithEmailAndPassword(this.auth, email, 'password123')
      : await signInAnonymously(this.auth);
    this.uid = cred.user.uid;
    return this;
  }
  async setRole(role: string | null) {
    await adminAuth.setCustomUserClaims(this.uid, role ? { role } : {});
    await this.auth.currentUser!.getIdToken(true);
  }
  call<N extends ApiName>(name: N, data: Api[N]['req']): Promise<Api[N]['res']> {
    return httpsCallable<Api[N]['req'], Api[N]['res']>(this.fns, name)(data).then((r) => r.data);
  }
}

/** The callable's error code and machine reason, for asserting failures. */
async function failure(promise: Promise<unknown>): Promise<{ code: string; reason: string; message: string }> {
  try {
    await promise;
  } catch (error) {
    const e = error as { code: string; message: string; details?: { reason?: string } };
    return { code: e.code, reason: e.details?.reason ?? '', message: e.message };
  }
  throw new Error('expected the call to fail');
}

async function postWebhook(event: object, secret = WEBHOOK_SECRET) {
  const body = JSON.stringify(event);
  return fetch(WEBHOOK_URL, { method: 'POST', headers: { 'content-type': 'application/json', 'x-sandbox-signature': signSandboxPayload(body, secret) }, body });
}

const players: Player[] = [];
function player(name: string) {
  const p = new Player(`${name}-${Math.random().toString(36).slice(2)}`);
  players.push(p);
  return p;
}

const future = Timestamp.fromMillis(Date.now() + 86_400_000);
const past = Timestamp.fromMillis(Date.now() - 1000);
const codeDoc = (over: Record<string, unknown>) => ({
  kind: 'open', rewards: [{ type: 'gems', id: 'gems', amount: 100 }], message: 'Welcome!', maxUses: null, uses: 0, active: true,
  startsAt: null, expiresAt: null, batchId: null, note: '', createdAt: Timestamp.now(), createdBy: 'test', ...over,
});
const productDoc = (over: Record<string, unknown>) => ({
  name: 'Chest of Gems', description: '', image: 'store_gems_550.png', rewards: [{ type: 'gems', id: 'gems', amount: 550 }],
  price: 499, currency: 'USD', salePrice: null, active: true, oncePerPlayer: false, appProductId: null, badge: '', sortOrder: 1,
  updatedAt: Timestamp.now(), ...over,
});

beforeAll(async () => {
  const batch = db.batch();
  batch.set(db.doc('codes/OPEN2026'), codeDoc({ code: 'OPEN2026' }));
  batch.set(db.doc('codes/ONLYONE'), codeDoc({ code: 'ONLYONE', kind: 'campaign', maxUses: 1 }));
  batch.set(db.doc('codes/OLDCODE'), codeDoc({ code: 'OLDCODE', expiresAt: past }));
  batch.set(db.doc('codes/SOONCODE'), codeDoc({ code: 'SOONCODE', startsAt: future }));
  batch.set(db.doc('codes/OFFCODE'), codeDoc({ code: 'OFFCODE', active: false }));
  batch.set(db.doc('products/gems_550'), productDoc({}));
  batch.set(db.doc('products/gems_sale'), productDoc({ name: 'Sale Gems', salePrice: 299 }));
  batch.set(db.doc('products/starter'), productDoc({ name: 'Starter Pack', oncePerPlayer: true, appProductId: 'com.thun.ninjavillagedefense.starter_pack', price: 299 }));
  batch.set(db.doc('products/hidden'), productDoc({ name: 'Hidden', active: false }));
  await batch.commit();
});

afterAll(async () => {
  await Promise.all(players.map((p) => signOut(p.auth).then(() => deleteApp(p.app))));
});

describe('redeemCode', () => {
  it('redeems a code once per player and creates the grant and history', async () => {
    const alice = await player('alice').signUp();
    const res = await alice.call('redeemCode', { code: 'open-2026' });
    expect(res).toEqual({ code: 'OPEN2026', rewards: [{ type: 'gems', id: 'gems', amount: 100 }], message: 'Welcome!' });

    const grant = (await db.doc(`users/${alice.uid}/grants/redeem_OPEN2026`).get()).data()!;
    expect(grant).toMatchObject({ kind: 'redeem', claimedAt: null, revokedAt: null, source: { code: 'OPEN2026' } });
    expect((await db.doc(`users/${alice.uid}/redemptions/OPEN2026`).get()).exists).toBe(true);
    expect((await db.doc(`codes/OPEN2026/redemptions/${alice.uid}`).get()).exists).toBe(true);

    expect(await failure(alice.call('redeemCode', { code: 'OPEN2026' }))).toMatchObject({ code: 'functions/already-exists', reason: 'already-redeemed' });
    // Open codes count uses outside the transaction.
    await new Promise((r) => setTimeout(r, 300));
    expect((await db.doc('codes/OPEN2026').get()).data()!.uses).toBe(1);
  });

  it('gives the same answer for unknown, expired, not started, off and used-up codes', async () => {
    const bob = await player('bob').signUp();
    const carol = await player('carol').signUp();
    await bob.call('redeemCode', { code: 'ONLYONE' });
    const answers = await Promise.all(['NOPE1234', 'OLDCODE', 'SOONCODE', 'OFFCODE', 'x'].map((code) => failure(carol.call('redeemCode', { code }))));
    answers.push(await failure(carol.call('redeemCode', { code: 'ONLYONE' })));
    for (const a of answers) expect(a).toMatchObject({ code: 'functions/not-found', reason: 'invalid-code' });
    expect(new Set(answers.map((a) => a.message)).size).toBe(1);
    expect((await db.doc('codes/ONLYONE').get()).data()!.uses).toBe(1);
  });

  it('limits attempts per player per hour', async () => {
    const dave = await player('dave').signUp();
    const results = [];
    for (let i = 0; i < 11; i++) results.push(await failure(dave.call('redeemCode', { code: `GUESS${i}X` })));
    expect(results.slice(0, 10).every((r) => r.reason === 'invalid-code')).toBe(true);
    expect(results[10]).toMatchObject({ code: 'functions/resource-exhausted', reason: 'too-many-attempts' });
  });

  it('refuses signed-out and banned players', async () => {
    const eve = player('eve');
    expect(await failure(eve.call('redeemCode', { code: 'OPEN2026' }))).toMatchObject({ code: 'functions/unauthenticated' });
    await eve.signUp();
    await db.doc(`users/${eve.uid}`).set({ banned: true });
    expect(await failure(eve.call('redeemCode', { code: 'OPEN2026' }))).toMatchObject({ code: 'functions/permission-denied', reason: 'banned' });
  });
});

describe('web shop — checkout, sandbox payment, webhook', () => {
  it('needs an email account and an active product', async () => {
    const anon = await player('anon').signUp();
    expect(await failure(anon.call('createCheckout', { productId: 'gems_550' }))).toMatchObject({ reason: 'link-email' });
    const buyer = await player('buyer0').signUp('buyer0@example.com');
    expect(await failure(buyer.call('createCheckout', { productId: 'hidden' }))).toMatchObject({ reason: 'no-product' });
    expect(await failure(buyer.call('createCheckout', { productId: 'nope' }))).toMatchObject({ reason: 'no-product' });
  });

  it('prices the order from the catalog and grants exactly once', async () => {
    const buyer = await player('buyer1').signUp('buyer1@example.com');
    const { orderId, url } = await buyer.call('createCheckout', { productId: 'gems_sale' });
    expect(url).toBe(`http://localhost:4321/shop/checkout/?order=${orderId}`);
    const order = (await db.doc(`orders/${orderId}`).get()).data()!;
    expect(order).toMatchObject({ uid: buyer.uid, email: 'buyer1@example.com', amount: 299, currency: 'USD', status: 'pending', provider: 'sandbox' });

    expect(await buyer.call('sandboxPay', { orderId, outcome: 'paid' })).toEqual({ status: 'paid' });
    expect(await buyer.call('sandboxPay', { orderId, outcome: 'paid' })).toEqual({ status: 'paid' });
    const grants = await db.collection(`users/${buyer.uid}/grants`).get();
    expect(grants.size).toBe(1);
    expect(grants.docs[0].id).toBe(`purchase_sandbox_sbx_${orderId}`);
    expect(grants.docs[0].data()).toMatchObject({ kind: 'purchase', rewards: [{ type: 'gems', id: 'gems', amount: 550 }] });

    // The provider retrying the same payment is a no-op.
    const replay = await postWebhook({ type: 'paid', paymentId: `sbx_${orderId}`, orderId, amount: 299, currency: 'USD' });
    expect(replay.status).toBe(200);
    expect(await replay.json()).toEqual({ results: ['duplicate'] });
    expect((await db.collection(`users/${buyer.uid}/grants`).get()).size).toBe(1);
  });

  it('rejects forged webhooks', async () => {
    const buyer = await player('buyer2').signUp('buyer2@example.com');
    const { orderId } = await buyer.call('createCheckout', { productId: 'gems_550' });
    const forged = await postWebhook({ type: 'paid', paymentId: 'pay_forged', orderId, amount: 499, currency: 'USD' }, 'wrong-secret');
    expect(forged.status).toBe(400);
    const unsigned = await fetch(WEBHOOK_URL, { method: 'POST', body: '{}' });
    expect(unsigned.status).toBe(400);
    expect((await db.doc(`orders/${orderId}`).get()).data()!.status).toBe('pending');
  });

  it('pays through the webhook, holds wrong amounts for review, and revokes on refund before collection', async () => {
    const buyer = await player('buyer3').signUp('buyer3@example.com');
    const { orderId } = await buyer.call('createCheckout', { productId: 'gems_550' });
    const paid = await postWebhook({ type: 'paid', paymentId: 'pay_1', orderId, amount: 499, currency: 'USD' });
    expect(await paid.json()).toEqual({ results: ['granted'] });
    const grantPath = `users/${buyer.uid}/grants/purchase_sandbox_pay_1`;
    expect((await db.doc(grantPath).get()).exists).toBe(true);

    const refund = await postWebhook({ type: 'refunded', paymentId: 'pay_1', orderId, amount: 499, currency: 'USD' });
    expect(await refund.json()).toEqual({ results: ['updated'] });
    expect((await db.doc(grantPath).get()).data()!.revokedAt).not.toBeNull();
    expect((await db.doc(`orders/${orderId}`).get()).data()!.status).toBe('refunded');

    const second = await buyer.call('createCheckout', { productId: 'gems_550' });
    const cheap = await postWebhook({ type: 'paid', paymentId: 'pay_2', orderId: second.orderId, amount: 1, currency: 'USD' });
    expect(await cheap.json()).toEqual({ results: ['review'] });
    expect((await db.collection(`users/${buyer.uid}/grants`).get()).size).toBe(1);
  });

  it('flags the player when a refund arrives after the gems were collected', async () => {
    const buyer = await player('buyer4').signUp('buyer4@example.com');
    const { orderId } = await buyer.call('createCheckout', { productId: 'gems_550' });
    await buyer.call('sandboxPay', { orderId, outcome: 'paid' });
    await db.doc(`users/${buyer.uid}/grants/purchase_sandbox_sbx_${orderId}`).update({ claimedAt: Timestamp.now() });
    await postWebhook({ type: 'chargeback', paymentId: `sbx_${orderId}`, orderId, amount: 499, currency: 'USD' });
    expect((await db.doc(`users/${buyer.uid}`).get()).data()!.reviewFlag).toMatchObject({ reason: 'chargeback', orderId });
  });

  it('sells once-only offers once, across the web shop and the app', async () => {
    const buyer = await player('buyer5').signUp('buyer5@example.com');
    const { orderId } = await buyer.call('createCheckout', { productId: 'starter' });
    await buyer.call('sandboxPay', { orderId, outcome: 'paid' });
    expect(await failure(buyer.call('createCheckout', { productId: 'starter' }))).toMatchObject({ reason: 'already-owned' });

    const appBuyer = await player('buyer6').signUp('buyer6@example.com');
    await db.doc(`users/${appBuyer.uid}`).set({ save: JSON.stringify({ Store: { StarterPackPurchased: true, PurchasedProductIds: [] } }) });
    expect(await failure(appBuyer.call('createCheckout', { productId: 'starter' }))).toMatchObject({ reason: 'already-owned' });
  });

  it('lets a buyer cancel, and only touch their own orders', async () => {
    const buyer = await player('buyer7').signUp('buyer7@example.com');
    const other = await player('buyer8').signUp('buyer8@example.com');
    const { orderId } = await buyer.call('createCheckout', { productId: 'gems_550' });
    expect(await failure(other.call('sandboxPay', { orderId, outcome: 'paid' }))).toMatchObject({ reason: 'no-order' });
    expect(await buyer.call('sandboxPay', { orderId, outcome: 'canceled' })).toEqual({ status: 'canceled' });
  });
});

describe('staff functions', () => {
  it('refuses players and enforces each role’s permissions', async () => {
    const nobody = await player('nobody').signUp('nobody@example.com');
    expect(await failure(nobody.call('adminSearchPlayers', { query: 'x' }))).toMatchObject({ code: 'functions/permission-denied', reason: 'not-staff' });

    const viewer = await player('viewer').signUp('viewer@example.com');
    await viewer.setRole('viewer');
    expect((await viewer.call('adminSearchPlayers', { query: 'nobody@example.com' })).players.map((p) => p.uid)).toEqual([nobody.uid]);
    expect(await failure(viewer.call('adminSendGift', { uid: nobody.uid, rewards: [{ type: 'gems', id: 'gems', amount: 5 }], title: 'Hi', message: '' })))
      .toMatchObject({ reason: 'not-allowed' });

    const support = await player('support').signUp('support@example.com');
    await support.setRole('support');
    const { grantId } = await support.call('adminSendGift', { uid: nobody.uid, rewards: [{ type: 'hero', id: 'monk', amount: 1 }], title: 'Sorry!', message: 'For the bug' });
    expect((await db.doc(`users/${nobody.uid}/grants/${grantId}`).get()).data()).toMatchObject({ kind: 'gift', title: 'Sorry!' });
    expect(await failure(support.call('adminSendGift', { uid: nobody.uid, rewards: [{ type: 'hero', id: 'goku', amount: 1 }], title: 'x', message: '' })))
      .toMatchObject({ code: 'functions/invalid-argument', reason: 'bad-rewards' });
    expect(await failure(support.call('adminCreateCodes', { code: 'NOPE', count: 0, prefix: '', kind: 'open', maxUses: null, rewards: [{ type: 'gems', id: 'gems', amount: 1 }], message: '', startsAt: null, expiresAt: null, note: '' })))
      .toMatchObject({ reason: 'not-allowed' });

    const audit = await db.collection('audit').where('targetUid', '==', nobody.uid).get();
    expect(audit.docs.map((d) => d.data().action)).toContain('gift.send');
  });

  it('bans: hides public entries, blocks redeeming, and unbans', async () => {
    const admin = await player('admin1').signUp('admin1@example.com');
    await admin.setRole('admin');
    const cheater = await player('cheater').signUp('cheater@example.com');
    await db.doc(`leaderboard/${cheater.uid}`).set({ displayName: 'Cheater', bestWave: 9999, bestKills: 1, updatedAt: Timestamp.now() });

    await admin.call('adminSetBan', { uid: cheater.uid, banned: true, reason: 'Edited save', days: 7 });
    expect((await db.doc(`leaderboard/${cheater.uid}`).get()).exists).toBe(false);
    const user = (await db.doc(`users/${cheater.uid}`).get()).data()!;
    expect(user.banned).toBe(true);
    expect(user.bannedUntil.toMillis()).toBeGreaterThan(Date.now() + 6 * 86_400_000);
    expect(await failure(cheater.call('redeemCode', { code: 'OPEN2026' }))).toMatchObject({ reason: 'banned' });

    const detail = await admin.call('adminGetPlayer', { uid: cheater.uid });
    expect(detail.moderation).toMatchObject({ banned: true, banReason: 'Edited save' });
    expect(detail.moderationLog[0]).toMatchObject({ action: 'ban', reason: 'Edited save' });
    expect(detail.auth?.email).toBe('cheater@example.com');

    await admin.call('adminSetBan', { uid: cheater.uid, banned: false, reason: '', days: null });
    expect((await admin.call('adminGetPlayer', { uid: cheater.uid })).moderation.banned).toBe(false);
  });

  it('creates custom and bulk codes that players can redeem', async () => {
    const admin = await player('admin2').signUp('admin2@example.com');
    await admin.setRole('admin');
    const base = { count: 0, prefix: '', kind: 'open' as const, maxUses: null, rewards: [{ type: 'crate' as const, id: 'silver', amount: 2 }], message: '', startsAt: null, expiresAt: null, note: 'test' };
    expect(await admin.call('adminCreateCodes', { ...base, code: 'launch-day' })).toEqual({ codes: ['LAUNCHDAY'], batchId: null });
    expect(await failure(admin.call('adminCreateCodes', { ...base, code: 'LAUNCHDAY' }))).toMatchObject({ reason: 'code-exists' });

    const bulk = await admin.call('adminCreateCodes', { ...base, code: '', count: 25, prefix: 'yt', kind: 'single' });
    expect(bulk.codes).toHaveLength(25);
    expect(new Set(bulk.codes).size).toBe(25);
    bulk.codes.forEach((c) => expect(c).toMatch(/^YT[A-Z0-9]{12}$/));

    const fan = await player('fan').signUp();
    expect((await fan.call('redeemCode', { code: bulk.codes[0] })).rewards).toEqual([{ type: 'crate', id: 'silver', amount: 2 }]);
    const fan2 = await player('fan2').signUp();
    expect(await failure(fan2.call('redeemCode', { code: bulk.codes[0] }))).toMatchObject({ reason: 'invalid-code' });

    await admin.call('adminSetCodeActive', { code: 'LAUNCHDAY', active: false });
    expect(await failure(fan2.call('redeemCode', { code: 'LAUNCHDAY' }))).toMatchObject({ reason: 'invalid-code' });
  });

  it('sends mail to everyone, collected once per player (and only by older accounts for compensation)', async () => {
    const admin = await player('admin3').signUp('admin3@example.com');
    await admin.setRole('admin');
    const veteran = await player('veteran').signUp();
    await new Promise((r) => setTimeout(r, 1100));
    const now = Date.now();
    const { id } = await admin.call('adminSendMail', { kind: 'compensation', title: 'Sorry for the downtime', message: '', rewards: [{ type: 'gems', id: 'gems', amount: 200 }], startsAt: now - 1000, expiresAt: now + 86_400_000, newPlayersToo: false });
    await new Promise((r) => setTimeout(r, 1100));
    const newbie = await player('newbie').signUp();

    expect(await veteran.call('collectMail', {})).toEqual({ created: 1 });
    expect(await veteran.call('collectMail', {})).toEqual({ created: 0 });
    expect((await db.doc(`users/${veteran.uid}/grants/mail_${id}`).get()).data()).toMatchObject({ kind: 'compensation' });
    expect(await newbie.call('collectMail', {})).toEqual({ created: 0 });

    await admin.call('adminSetMailActive', { id, active: false });
  });

  it('manages roles but never your own', async () => {
    const admin = await player('admin4').signUp('admin4@example.com');
    await admin.setRole('admin');
    const helper = await player('helper').signUp('helper@example.com');
    const { uid } = await admin.call('adminSetRole', { email: 'HELPER@example.com', role: 'support' });
    expect(uid).toBe(helper.uid);
    expect((await adminAuth.getUser(uid)).customClaims).toEqual({ role: 'support' });
    expect((await db.doc(`staff/${uid}`).get()).data()).toMatchObject({ role: 'support' });
    await admin.call('adminSetRole', { email: 'helper@example.com', role: null });
    expect((await adminAuth.getUser(uid)).customClaims ?? {}).toEqual({});
    expect(await failure(admin.call('adminSetRole', { email: 'admin4@example.com', role: 'viewer' }))).toMatchObject({ reason: 'self' });
  });

  it('handles account deletion requests end to end', async () => {
    const admin = await player('admin5').signUp('admin5@example.com');
    await admin.setRole('admin');
    const leaver = await player('leaver').signUp('leaver@example.com');
    await leaver.call('redeemCode', { code: 'OPEN2026' });
    const { orderId } = await leaver.call('createCheckout', { productId: 'gems_550' });
    await leaver.call('sandboxPay', { orderId, outcome: 'paid' });

    expect(await failure(leaver.call('requestAccountDeletion', { confirm: 'nope' as 'DELETE' }))).toMatchObject({ reason: 'confirm' });
    expect(await leaver.call('requestAccountDeletion', { confirm: 'DELETE' })).toEqual({ status: 'pending' });
    expect(await leaver.call('cancelAccountDeletion', {})).toEqual({ status: 'canceled' });
    expect(await leaver.call('requestAccountDeletion', { confirm: 'DELETE' })).toEqual({ status: 'pending' });

    await admin.call('adminProcessDeletion', { uid: leaver.uid, action: 'delete', note: 'Requested on the website' });
    await expect(adminAuth.getUser(leaver.uid)).rejects.toMatchObject({ code: 'auth/user-not-found' });
    expect((await db.collection(`users/${leaver.uid}/grants`).get()).size).toBe(0);
    expect((await db.doc(`codes/OPEN2026/redemptions/${leaver.uid}`).get()).exists).toBe(false);
    expect((await db.doc(`orders/${orderId}`).get()).data()).toMatchObject({ status: 'paid', email: null, accountDeleted: true });
    expect((await db.doc(`deletionRequests/${leaver.uid}`).get()).data()).toMatchObject({ status: 'done', email: null });
  });

  it('counts new players, redemptions and revenue for the dashboard', async () => {
    const today = new Date().toISOString().slice(0, 10);
    await new Promise((r) => setTimeout(r, 500));
    const stats = (await db.doc(`stats/${today}`).get()).data()!;
    expect(stats.newPlayers).toBeGreaterThan(10);
    expect(stats.redemptions).toBeGreaterThan(2);
    expect(stats.revenue.USD).toBeGreaterThan(0);
  });
});
