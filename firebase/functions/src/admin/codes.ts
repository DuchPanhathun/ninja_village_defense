import { randomBytes } from 'node:crypto';
import type { CreateCodesInput } from '../shared/api.js';
import { MAX_BULK_CODES, generateCode, isValidCode, maxUsesFor, normalizeCode } from '../shared/codes.js';
import { validateRewards } from '../shared/rewards.js';
import type { CodeDoc, CodeKind } from '../shared/types.js';
import { bool, fail, int, optionalMillis, staffCallable, str, type Untrusted } from '../lib/callable.js';
import { Timestamp, db, tsOrNull } from '../lib/firebase.js';
import { audit } from '../lib/records.js';

const KINDS: readonly CodeKind[] = ['single', 'campaign', 'open'];
const GENERATED_LENGTH = 12;

function readInput(data: Untrusted<CreateCodesInput>) {
  const kind = data.kind as CodeKind;
  if (!KINDS.includes(kind)) fail('invalid-argument', 'bad-input', 'Choose a code type.');
  const requestedUses = kind === 'campaign' ? int(data.maxUses, 'Use limit', { min: 1, max: 10_000_000 }) : null;
  const startsAt = optionalMillis(data.startsAt, 'Start');
  const expiresAt = optionalMillis(data.expiresAt, 'End');
  if (expiresAt !== null && expiresAt <= Math.max(Date.now(), startsAt ?? 0)) fail('invalid-argument', 'bad-input', 'The end must be in the future and after the start.');
  return {
    kind,
    maxUses: maxUsesFor(kind, requestedUses),
    rewards: validateRewards(data.rewards),
    message: str(data.message ?? '', 'Message', { max: 200 }),
    note: str(data.note ?? '', 'Note', { max: 200 }),
    startsAt: tsOrNull(startsAt),
    expiresAt: tsOrNull(expiresAt),
  };
}

/**
 * Creates one custom code (e.g. SAKURA2026) or generates up to 5,000 random ones in a batch (the website turns
 * the returned list into a CSV). Codes are stored in canonical form: upper case, no dashes.
 */
export const adminCreateCodes = staffCallable('adminCreateCodes', 'codes.write', async (data, staff) => {
  const input = readInput(data);
  const custom = normalizeCode(typeof data.code === 'string' ? data.code : '');
  const base = (code: string, batchId: string | null): CodeDoc<Timestamp> => ({
    code,
    ...input,
    uses: 0,
    active: true,
    batchId,
    createdAt: Timestamp.now(),
    createdBy: staff.email ?? staff.uid,
  });

  if (custom !== '') {
    if (!isValidCode(custom)) fail('invalid-argument', 'bad-code', 'Codes are 4–24 letters and digits.');
    const ref = db.collection('codes').doc(custom);
    await db.runTransaction(async (tx) => {
      if ((await tx.get(ref)).exists) fail('already-exists', 'code-exists', 'This code already exists.');
      tx.create(ref, base(custom, null));
      audit(tx, staff, 'code.create', null, { code: custom, kind: input.kind, maxUses: input.maxUses, rewards: input.rewards });
    });
    return { codes: [custom], batchId: null };
  }

  const count = int(data.count, 'How many', { min: 1, max: MAX_BULK_CODES });
  const prefix = normalizeCode(typeof data.prefix === 'string' ? data.prefix : '').slice(0, 8);
  const batchId = `batch_${Date.now().toString(36)}`;
  const created: string[] = [];
  let pending = Array.from({ length: count }, () => generateCode(GENERATED_LENGTH, prefix, randomBytes));

  // create() fails on an existing id; the rare collisions are simply generated again.
  for (let attempt = 0; attempt < 3 && pending.length > 0; attempt++) {
    const failed: string[] = [];
    const writer = db.bulkWriter();
    writer.onWriteError(() => false);
    const writes = pending.map((code) =>
      writer.create(db.collection('codes').doc(code), base(code, batchId)).then(
        () => created.push(code),
        () => failed.push(code),
      ),
    );
    await writer.close();
    await Promise.all(writes);
    pending = failed.map(() => generateCode(GENERATED_LENGTH, prefix, randomBytes));
  }
  if (created.length === 0) fail('internal', 'internal', 'Could not create the codes.');

  const batch = db.batch();
  audit(batch, staff, 'code.bulk-create', null, { batchId, count: created.length, prefix, kind: input.kind, rewards: input.rewards });
  await batch.commit();
  return { codes: created, batchId };
});

export const adminSetCodeActive = staffCallable('adminSetCodeActive', 'codes.write', async (data, staff) => {
  const code = normalizeCode(str(data.code, 'Code', { min: 1, max: 64 }));
  const active = bool(data.active, 'Active');
  const ref = db.collection('codes').doc(code);
  await db.runTransaction(async (tx) => {
    if (!(await tx.get(ref)).exists) fail('not-found', 'no-code', 'Code not found.');
    tx.update(ref, { active });
    audit(tx, staff, active ? 'code.enable' : 'code.disable', null, { code });
  });
  return { ok: true };
});
