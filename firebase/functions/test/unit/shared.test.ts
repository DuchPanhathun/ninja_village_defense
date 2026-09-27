import { describe, expect, it } from 'vitest';
import { evaluateCode, formatCode, generateCode, isValidCode, maxUsesFor, normalizeCode, CODE_ALPHABET } from '../../src/shared/codes.js';
import { describeReward, validateRewards, RewardError } from '../../src/shared/rewards.js';
import { can, roleNeedsMfa } from '../../src/shared/roles.js';
import { formatMoney, parseMoney, minorToInput } from '../../src/shared/money.js';

describe('codes', () => {
  it('normalizes what players type', () => {
    expect(normalizeCode(' sakura-2026 ')).toBe('SAKURA2026');
    expect(normalizeCode('abcd efgh-jkmn')).toBe('ABCDEFGHJKMN');
    expect(isValidCode('ABC')).toBe(false);
    expect(isValidCode('ABCD')).toBe(true);
    expect(isValidCode('A'.repeat(25))).toBe(false);
  });

  it('formats long codes in groups of four', () => {
    expect(formatCode('ABCDEFGHJKMN')).toBe('ABCD-EFGH-JKMN');
    expect(formatCode('YTABCDEFGHJKMN')).toBe('YT-ABCD-EFGH-JKMN');
    expect(formatCode('ABCD')).toBe('ABCD');
  });

  it('generates codes of the right length from the safe alphabet', () => {
    let i = 0;
    const bytes = (n: number) => Uint8Array.from({ length: n }, () => (i++ * 37) % 256);
    const code = generateCode(10, 'ev-', bytes);
    expect(code).toMatch(/^EV[A-Z0-9]{10}$/);
    for (const ch of code.slice(2)) expect(CODE_ALPHABET).toContain(ch);
  });

  it('skips biased bytes (rejection sampling)', () => {
    // Bytes >= 248 (31 × 8) would favour the first letters, so they must be dropped, not wrapped round.
    const source = (n: number) => Uint8Array.from({ length: n }, (_, i) => (i % 2 === 0 ? 250 : 0));
    expect(generateCode(4, '', source)).toBe('AAAA');
  });

  const base = { active: true, startsAt: null, expiresAt: null, maxUses: null, uses: 0 };
  it('accepts a live code and rejects everything else with one answer', () => {
    const now = 1_000_000;
    expect(evaluateCode(base, now, false)).toBe('ok');
    expect(evaluateCode(null, now, false)).toBe('invalid');
    expect(evaluateCode({ ...base, active: false }, now, false)).toBe('invalid');
    expect(evaluateCode({ ...base, startsAt: now + 1 }, now, false)).toBe('invalid');
    expect(evaluateCode({ ...base, expiresAt: now }, now, false)).toBe('invalid');
    expect(evaluateCode({ ...base, maxUses: 5, uses: 5 }, now, false)).toBe('invalid');
    expect(evaluateCode({ ...base, maxUses: 5, uses: 4 }, now, false)).toBe('ok');
  });

  it('tells a player they already used a live code, but not a dead one', () => {
    expect(evaluateCode(base, 0, true)).toBe('already-redeemed');
    expect(evaluateCode({ ...base, active: false }, 0, true)).toBe('invalid');
  });

  it('derives the use limit from the kind', () => {
    expect(maxUsesFor('single', 99)).toBe(1);
    expect(maxUsesFor('open', 99)).toBeNull();
    expect(maxUsesFor('campaign', 99)).toBe(99);
  });
});

describe('rewards', () => {
  it('accepts known rewards and merges duplicates', () => {
    expect(validateRewards([{ type: 'gems', id: 'gems', amount: 100 }, { type: 'gems', amount: 50 }, { type: 'hero', id: 'beast_ninja', amount: 1 }]))
      .toEqual([{ type: 'gems', id: 'gems', amount: 150 }, { type: 'hero', id: 'beast_ninja', amount: 1 }]);
  });

  it('rejects unknown types, ids and bad amounts', () => {
    expect(() => validateRewards([])).toThrow(RewardError);
    expect(() => validateRewards([{ type: 'diamonds', id: 'x', amount: 1 }])).toThrow(/Unknown reward type/);
    expect(() => validateRewards([{ type: 'hero', id: 'goku', amount: 1 }])).toThrow(/Unknown hero/);
    expect(() => validateRewards([{ type: 'gems', amount: 0 }])).toThrow(/at least 1/);
    expect(() => validateRewards([{ type: 'gems', amount: 1.5 }])).toThrow(/whole number/);
    expect(() => validateRewards([{ type: 'hero', id: 'monk', amount: 2 }])).toThrow(/at most 1/);
    expect(() => validateRewards([{ type: 'gems', amount: 60_000 }, { type: 'gems', amount: 60_000 }])).toThrow(/at most/);
    expect(() => validateRewards(Array.from({ length: 11 }, () => ({ type: 'coins', amount: 1 })))).toThrow(/At most 10/);
  });

  it('describes rewards for people', () => {
    expect(describeReward({ type: 'gems', id: 'gems', amount: 1200 })).toBe('1,200 Gems');
    expect(describeReward({ type: 'crate', id: 'silver', amount: 3 })).toBe('3 × Silver Crate');
    expect(describeReward({ type: 'hero', id: 'beast_ninja', amount: 1 })).toBe('Beast Ninja');
  });
});

describe('roles', () => {
  it('gives each role its permissions', () => {
    expect(can('viewer', 'read')).toBe(true);
    expect(can('viewer', 'players.gift')).toBe(false);
    expect(can('support', 'players.ban')).toBe(true);
    expect(can('support', 'codes.write')).toBe(false);
    expect(can('admin', 'staff.write')).toBe(true);
    expect(can(null, 'read')).toBe(false);
    expect(roleNeedsMfa('admin')).toBe(true);
    expect(roleNeedsMfa('support')).toBe(false);
  });
});

describe('money', () => {
  it('parses and formats minor units without floats', () => {
    expect(parseMoney('4.99', 'USD')).toBe(499);
    expect(parseMoney('10', 'USD')).toBe(1000);
    expect(parseMoney('0.1', 'USD')).toBe(10);
    expect(parseMoney('4.999', 'USD')).toBeNull();
    expect(parseMoney('-1', 'USD')).toBeNull();
    expect(parseMoney('500', 'JPY')).toBe(500);
    expect(parseMoney('5.5', 'JPY')).toBeNull();
    expect(formatMoney(499, 'USD')).toBe('$4.99');
    expect(minorToInput(1999, 'USD')).toBe('19.99');
  });
});
