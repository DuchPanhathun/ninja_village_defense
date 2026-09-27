import { REWARD_TYPES, type Reward, type RewardType } from './types.js';

/** One thing a reward can point at. `icon` is a file name under the website's /art/ folder. */
export interface CatalogEntry {
  id: string;
  name: string;
  icon: string;
  /** A bookkeeping flag for the game, not something to show players as a reward. */
  internal?: boolean;
}

export interface RewardTypeInfo {
  label: string;
  /** Largest amount one reward line may carry (a typo guard, not a game rule). */
  maxAmount: number;
  entries: CatalogEntry[];
}

/**
 * Everything the web can give, with the game's ids (Assets/_Project/Data). The game skips ids it doesn't know,
 * so a new hero only needs adding here once the app version that has it is live.
 */
export const REWARD_CATALOG: Record<RewardType, RewardTypeInfo> = {
  gems: { label: 'Gems', maxAmount: 100_000, entries: [{ id: 'gems', name: 'Gems', icon: 'store_gems_100.png' }] },
  coins: { label: 'Coins', maxAmount: 10_000_000, entries: [{ id: 'coins', name: 'Coins', icon: 'pickup_coin.png' }] },
  hero: {
    label: 'Hero',
    maxAmount: 1,
    entries: [
      { id: 'samurai', name: 'Samurai', icon: 'hero_samurai.png' },
      { id: 'assassin', name: 'Assassin', icon: 'hero_assassin.png' },
      { id: 'monk', name: 'Monk', icon: 'hero_monk.png' },
      { id: 'mage_ninja', name: 'Mage Ninja', icon: 'hero_mage_ninja.png' },
      { id: 'beast_ninja', name: 'Beast Ninja', icon: 'hero_beast_ninja.png' },
    ],
  },
  pet: {
    label: 'Pet',
    maxAmount: 1,
    entries: [
      { id: 'wolf', name: 'Wolf', icon: 'pet_wolf.png' },
      { id: 'monkey', name: 'Monkey', icon: 'pet_monkey.png' },
      { id: 'fox', name: 'Fox', icon: 'pet_fox.png' },
      { id: 'hawk', name: 'Hawk', icon: 'pet_hawk.png' },
      { id: 'dragon', name: 'Baby Dragon', icon: 'pet_dragon.png' },
    ],
  },
  skin: {
    label: 'Skin',
    maxAmount: 1,
    entries: [
      { id: 'skin_samurai_gold', name: 'Golden Samurai', icon: 'skin_samurai_gold.png' },
      { id: 'skin_samurai_oni', name: 'Oni Samurai', icon: 'skin_samurai_oni.png' },
      { id: 'skin_assassin_sakura', name: 'Sakura Assassin', icon: 'skin_assassin_sakura.png' },
      { id: 'skin_assassin_crimson', name: 'Crimson Assassin', icon: 'skin_assassin_crimson.png' },
      { id: 'skin_monk_jade', name: 'Jade Monk', icon: 'skin_monk_jade.png' },
      { id: 'skin_mage_frost', name: 'Frost Mage', icon: 'skin_mage_frost.png' },
      { id: 'skin_beast_shadow', name: 'Shadow Beastmaster', icon: 'skin_beast_shadow.png' },
    ],
  },
  crate: {
    label: 'Crate',
    maxAmount: 50,
    entries: [
      { id: 'wood', name: 'Wooden Crate', icon: 'chest_wood_closed.png' },
      { id: 'silver', name: 'Silver Crate', icon: 'chest_silver_closed.png' },
      { id: 'surprise', name: 'Surprise Box', icon: 'chest_surprise_closed.png' },
    ],
  },
  entitlement: {
    label: 'Unlock',
    maxAmount: 1,
    entries: [
      { id: 'remove_ads', name: 'Remove Ads', icon: 'store_remove_ads.png' },
      { id: 'starter_pack', name: 'Starter Pack bought', icon: 'store_starter_pack.png', internal: true },
      { id: 'battle_pass_premium', name: 'Premium Battle Pass', icon: 'store_battle_pass_premium.png' },
    ],
  },
};

export const MAX_REWARD_LINES = 10;

export class RewardError extends Error {}

export function isRewardType(value: unknown): value is RewardType {
  return typeof value === 'string' && (REWARD_TYPES as readonly string[]).includes(value);
}

export function findEntry(type: RewardType, id: string): CatalogEntry | undefined {
  return REWARD_CATALOG[type].entries.find((e) => e.id === id);
}

/**
 * Checks untrusted input (a callable payload) and returns clean reward lines: known type and id, whole positive
 * amount within the type's limit, at most {@link MAX_REWARD_LINES} lines, same type+id merged. Throws RewardError.
 */
export function validateRewards(input: unknown): Reward[] {
  if (!Array.isArray(input) || input.length === 0) throw new RewardError('Add at least one reward.');
  if (input.length > MAX_REWARD_LINES) throw new RewardError(`At most ${MAX_REWARD_LINES} rewards.`);

  const merged = new Map<string, Reward>();
  for (const raw of input) {
    if (typeof raw !== 'object' || raw === null) throw new RewardError('Invalid reward.');
    const { type, id, amount } = raw as Record<string, unknown>;
    if (!isRewardType(type)) throw new RewardError(`Unknown reward type "${String(type)}".`);
    const info = REWARD_CATALOG[type];
    const cleanId = typeof id === 'string' && id !== '' ? id : info.entries.length === 1 ? info.entries[0].id : '';
    if (!findEntry(type, cleanId)) throw new RewardError(`Unknown ${info.label.toLowerCase()} "${cleanId}".`);
    if (typeof amount !== 'number' || !Number.isInteger(amount) || amount < 1)
      throw new RewardError(`${info.label}: the amount must be a whole number of at least 1.`);

    const key = `${type}:${cleanId}`;
    const total = (merged.get(key)?.amount ?? 0) + amount;
    if (total > info.maxAmount) throw new RewardError(`${info.label}: at most ${info.maxAmount.toLocaleString('en-US')}.`);
    merged.set(key, { type, id: cleanId, amount: total });
  }
  return [...merged.values()];
}

/** "550 Gems", "Beast Ninja", "3 × Silver Crate". */
export function describeReward(reward: Reward): string {
  const entry = findEntry(reward.type, reward.id);
  const name = entry?.name ?? reward.id;
  if (reward.type === 'gems' || reward.type === 'coins') return `${reward.amount.toLocaleString('en-US')} ${name}`;
  return reward.amount > 1 ? `${reward.amount} × ${name}` : name;
}

export function describeRewards(rewards: readonly Reward[]): string {
  return rewards.map(describeReward).join(', ');
}

export function rewardIcon(reward: Reward): string {
  return findEntry(reward.type, reward.id)?.icon ?? 'icon_item_scroll.png';
}
