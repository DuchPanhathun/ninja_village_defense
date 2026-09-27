import type { CodeKind } from './types.js';

/** Letters and digits that can't be misread (no 0/O, 1/I/L). Used for generated codes. */
export const CODE_ALPHABET = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
export const CODE_MIN_LENGTH = 4;
export const CODE_MAX_LENGTH = 24;
export const MAX_BULK_CODES = 5000;

/**
 * The canonical form of a code: upper case, only A–Z and 0–9. "sakura-2026 " and "SAKURA2026" are the same code,
 * so players can type generated codes with or without the dashes they're printed with.
 */
export function normalizeCode(raw: string): string {
  return raw.toUpperCase().replace(/[^A-Z0-9]/g, '');
}

export function isValidCode(code: string): boolean {
  return code.length >= CODE_MIN_LENGTH && code.length <= CODE_MAX_LENGTH && /^[A-Z0-9]+$/.test(code);
}

/**
 * Generated codes for print, in groups of four from the end so a prefix stands alone: "YTABCDEFGHJKMN" →
 * "YT-ABCD-EFGH-JKMN" (normalizeCode drops the dashes again). Custom codes are shown as typed.
 */
export function formatCode(code: string): string {
  const groups: string[] = [];
  for (let end = code.length; end > 0; end -= 4) groups.unshift(code.slice(Math.max(0, end - 4), end));
  return groups.join('-');
}

/** A random code of `length` characters after `prefix`, from uniform random bytes (rejection sampling, no bias). */
export function generateCode(length: number, prefix: string, randomBytes: (n: number) => Uint8Array): string {
  const limit = 256 - (256 % CODE_ALPHABET.length);
  let body = '';
  while (body.length < length) {
    for (const byte of randomBytes(length * 2)) {
      if (byte >= limit) continue;
      body += CODE_ALPHABET[byte % CODE_ALPHABET.length];
      if (body.length === length) break;
    }
  }
  return normalizeCode(prefix) + body;
}

export function maxUsesFor(kind: CodeKind, requested: number | null): number | null {
  if (kind === 'single') return 1;
  if (kind === 'open') return null;
  return requested;
}

/** The parts of a code document that decide whether it can be redeemed now (times in epoch ms). */
export interface CodeState {
  active: boolean;
  startsAt: number | null;
  expiresAt: number | null;
  maxUses: number | null;
  uses: number;
}

export type CodeVerdict = 'ok' | 'invalid' | 'already-redeemed';

/**
 * Whether this player may redeem the code now. Missing, switched off, not started, expired and used up all give
 * 'invalid' — one answer, so guessing reveals nothing about which codes exist.
 */
export function evaluateCode(state: CodeState | null, nowMs: number, redeemedByPlayer: boolean): CodeVerdict {
  if (!state || !state.active) return 'invalid';
  if (state.startsAt !== null && nowMs < state.startsAt) return 'invalid';
  if (state.expiresAt !== null && nowMs >= state.expiresAt) return 'invalid';
  if (redeemedByPlayer) return 'already-redeemed';
  if (state.maxUses !== null && state.uses >= state.maxUses) return 'invalid';
  return 'ok';
}
