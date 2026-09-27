import { isRole } from '../shared/roles.js';
import { isCurrencyCode } from '../shared/money.js';
import { validateRewards } from '../shared/rewards.js';
import type { MailDoc, ProductDoc, StaffDoc } from '../shared/types.js';
import { bool, fail, int, staffCallable, str } from '../lib/callable.js';
import { Timestamp, adminAuth, db } from '../lib/firebase.js';
import { audit } from '../lib/records.js';

/** Adds or updates a web-shop product. Prices are minor units (499 = $4.99). */
export const adminSaveProduct = staffCallable('adminSaveProduct', 'shop.write', async (data, staff) => {
  const id = str(data.id, 'Product ID', { min: 2, max: 40 });
  if (!/^[a-z0-9_]+$/.test(id)) fail('invalid-argument', 'bad-input', 'Product ID: lower-case letters, digits and _ only.');
  const currency = str(data.currency, 'Currency', { min: 3, max: 3 }).toUpperCase();
  if (!isCurrencyCode(currency)) fail('invalid-argument', 'bad-input', 'Unknown currency code.');
  const price = int(data.price, 'Price', { min: 1, max: 100_000_000 });
  const salePrice = data.salePrice === null || data.salePrice === undefined ? null : int(data.salePrice, 'Sale price', { min: 1, max: price - 1 });
  const image = str(data.image, 'Picture', { min: 1, max: 80 });
  if (!/^[a-z0-9_]+\.png$/.test(image)) fail('invalid-argument', 'bad-input', 'Picture: a file name from /art/, e.g. store_gems_550.png.');
  const appProductId = typeof data.appProductId === 'string' && data.appProductId.trim() !== '' ? str(data.appProductId, 'App product ID', { max: 120 }) : null;

  const product: ProductDoc<Timestamp> = {
    name: str(data.name, 'Name', { min: 1, max: 60 }),
    description: str(data.description ?? '', 'Description', { max: 300 }),
    image,
    rewards: validateRewards(data.rewards),
    price,
    currency,
    salePrice,
    active: bool(data.active, 'Active'),
    oncePerPlayer: bool(data.oncePerPlayer, 'Once per player'),
    appProductId,
    badge: str(data.badge ?? '', 'Badge', { max: 20 }),
    sortOrder: int(data.sortOrder ?? 0, 'Sort order', { min: 0, max: 9999 }),
    updatedAt: Timestamp.now(),
  };
  const batch = db.batch();
  batch.set(db.collection('products').doc(id), product);
  audit(batch, staff, 'product.save', null, { id, price, salePrice, currency, active: product.active });
  await batch.commit();
  return { id };
});

/** Gift to everyone (e.g. compensation after downtime): one document the game turns into a grant per player. */
export const adminSendMail = staffCallable('adminSendMail', 'mail.write', async (data, staff) => {
  const startsAt = int(data.startsAt, 'Start', { min: 0 });
  const expiresAt = int(data.expiresAt, 'End', { min: startsAt + 60_000 });
  if (expiresAt <= Date.now()) fail('invalid-argument', 'bad-input', 'The end must be in the future.');
  const mail: MailDoc<Timestamp> = {
    kind: data.kind === 'compensation' ? 'compensation' : 'gift',
    title: str(data.title, 'Title', { min: 1, max: 60 }),
    message: str(data.message ?? '', 'Message', { max: 300 }),
    rewards: validateRewards(data.rewards),
    active: true,
    startsAt: Timestamp.fromMillis(startsAt),
    expiresAt: Timestamp.fromMillis(expiresAt),
    newPlayersToo: bool(data.newPlayersToo, 'New players too'),
    createdAt: Timestamp.now(),
    createdBy: staff.email ?? staff.uid,
    collected: 0,
  };
  const ref = db.collection('mail').doc();
  const batch = db.batch();
  batch.create(ref, mail);
  audit(batch, staff, 'mail.send', null, { id: ref.id, title: mail.title, rewards: mail.rewards, kind: mail.kind });
  await batch.commit();
  return { id: ref.id };
});

export const adminSetMailActive = staffCallable('adminSetMailActive', 'mail.write', async (data, staff) => {
  const id = str(data.id, 'Mail', { min: 1, max: 64 });
  const active = bool(data.active, 'Active');
  const ref = db.collection('mail').doc(id);
  await db.runTransaction(async (tx) => {
    if (!(await tx.get(ref)).exists) fail('not-found', 'no-mail', 'Mail not found.');
    tx.update(ref, { active });
    audit(tx, staff, active ? 'mail.enable' : 'mail.disable', null, { id });
  });
  return { ok: true };
});

/**
 * Gives or removes a staff role (Firebase custom claim `role`, mirrored in staff/{uid} for the list). Removing a
 * role also signs that account out everywhere. Admins can't change their own role, so nobody locks themselves out.
 */
export const adminSetRole = staffCallable('adminSetRole', 'staff.write', async (data, staff) => {
  const email = str(data.email, 'Email', { min: 3, max: 200 }).toLowerCase();
  const role = data.role === null ? null : data.role;
  if (role !== null && !isRole(role)) fail('invalid-argument', 'bad-input', 'Unknown role.');
  const record = await adminAuth.getUserByEmail(email).catch(() => null);
  if (!record) fail('not-found', 'no-account', 'No account with this email. They must sign in once first.');
  if (record.uid === staff.uid) fail('failed-precondition', 'self', 'You can’t change your own role.');

  const claims = { ...(record.customClaims ?? {}) };
  if (role) claims.role = role;
  else delete claims.role;
  await adminAuth.setCustomUserClaims(record.uid, claims);
  if (!role) await adminAuth.revokeRefreshTokens(record.uid);

  const batch = db.batch();
  const ref = db.collection('staff').doc(record.uid);
  if (role) batch.set(ref, { email: record.email ?? email, role, updatedAt: Timestamp.now(), updatedBy: staff.email ?? staff.uid } satisfies StaffDoc<Timestamp>);
  else batch.delete(ref);
  audit(batch, staff, role ? 'staff.set-role' : 'staff.remove', record.uid, { email, role });
  await batch.commit();
  return { uid: record.uid };
});
