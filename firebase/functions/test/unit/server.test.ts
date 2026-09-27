import { describe, expect, it } from 'vitest';
import { Timestamp } from 'firebase-admin/firestore';
import { nextWindow } from '../../src/lib/rateLimit.js';
import { isBanned, ownedInApp, readSaveStore } from '../../src/lib/players.js';
import { sandboxProvider, signSandboxPayload, verifySandboxSignature, WebhookSignatureError } from '../../src/payments/providers.js';
import { purchaseGrantId } from '../../src/payments/apply.js';

describe('rate limit window', () => {
  const HOUR = 3_600_000;
  it('allows up to the limit per window, then resets', () => {
    let state = null;
    for (let i = 0; i < 3; i++) {
      const r = nextWindow(state, 1000 + i, 3, HOUR);
      expect(r.allowed).toBe(true);
      state = r.state;
    }
    expect(nextWindow(state, 2000, 3, HOUR).allowed).toBe(false);
    const later = nextWindow(state, 1000 + HOUR, 3, HOUR);
    expect(later.allowed).toBe(true);
    expect(later.state.count).toBe(1);
  });
});

describe('players', () => {
  it('reads bans like the rules do', () => {
    const now = Date.now();
    expect(isBanned(undefined)).toBe(false);
    expect(isBanned({ banned: true })).toBe(true);
    expect(isBanned({ banned: true, bannedUntil: null })).toBe(true);
    expect(isBanned({ banned: true, bannedUntil: Timestamp.fromMillis(now + 60_000) }, now)).toBe(true);
    expect(isBanned({ banned: true, bannedUntil: Timestamp.fromMillis(now - 1) }, now)).toBe(false);
    expect(isBanned({ banned: false })).toBe(false);
  });

  it('reads app purchases from the Unity save JSON', () => {
    const save = JSON.stringify({ Version: 2, Store: { AdsRemoved: true, PurchasedProductIds: ['com.thun.ninjavillagedefense.remove_ads'], StarterPackPurchased: true, ProcessedTransactionIds: ['a', 'b'], PremiumPassSeasons: [] } });
    const store = readSaveStore(save);
    expect(store).toEqual({ starterPackPurchased: true, adsRemoved: true, purchasedProductIds: ['com.thun.ninjavillagedefense.remove_ads'], premiumPassSeasons: [], transactions: 2 });
    expect(ownedInApp(store, 'com.thun.ninjavillagedefense.starter_pack')).toBe(true);
    expect(ownedInApp(store, 'com.thun.ninjavillagedefense.remove_ads')).toBe(true);
    expect(ownedInApp(store, 'com.thun.ninjavillagedefense.gems_100')).toBe(false);
    expect(readSaveStore('not json')).toBeNull();
    expect(readSaveStore(undefined)).toBeNull();
  });
});

describe('sandbox webhook signatures', () => {
  const secret = 'shh';
  const body = JSON.stringify({ type: 'paid', paymentId: 'p1', orderId: 'o1', amount: 499, currency: 'USD' });

  it('accepts a fresh, correctly signed payload', () => {
    const header = signSandboxPayload(body, secret);
    expect(verifySandboxSignature(body, header, secret)).toBe(true);
    const events = sandboxProvider.parseWebhook(Buffer.from(body), { 'x-sandbox-signature': header }, secret);
    expect(events).toEqual([{ type: 'paid', provider: 'sandbox', paymentId: 'p1', orderId: 'o1', amount: 499, currency: 'USD' }]);
  });

  it('rejects wrong secrets, edited bodies and old timestamps', () => {
    const header = signSandboxPayload(body, secret);
    expect(verifySandboxSignature(body, header, 'other')).toBe(false);
    expect(verifySandboxSignature(body.replace('499', '1'), header, secret)).toBe(false);
    expect(verifySandboxSignature(body, signSandboxPayload(body, secret, 1000), secret)).toBe(false);
    expect(verifySandboxSignature(body, undefined, secret)).toBe(false);
    expect(verifySandboxSignature(body, 't=1,v1=zz', secret)).toBe(false);
    expect(() => sandboxProvider.parseWebhook(Buffer.from(body), {}, secret)).toThrow(WebhookSignatureError);
  });

  it('keys purchase grants by provider payment id', () => {
    expect(purchaseGrantId('sandbox', 'pi_123/../x')).toBe('purchase_sandbox_pi_123____x');
  });
});
