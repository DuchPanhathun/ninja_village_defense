import { describeReward, findEntry, rewardIcon } from '@shared/rewards';
import type { Reward } from '@shared/types';

/**
 * Rewards as little chips with the game's icons, e.g. [💎 550 Gems] [🧰 Silver Crate]. Internal flags (like
 * "starter pack bought") are hidden from players; the admin site passes `showInternal`.
 */
export default function RewardChips({ rewards, showInternal = false }: { rewards: readonly Reward[]; showInternal?: boolean }) {
  const shown = showInternal ? rewards : rewards.filter((r) => !findEntry(r.type, r.id)?.internal);
  return (
    <span class="chips">
      {shown.map((reward) => (
        <span class="chip" key={`${reward.type}:${reward.id}`}>
          <img src={`/art/${rewardIcon(reward)}`} alt="" width="24" height="24" loading="lazy" />
          {describeReward(reward)}
        </span>
      ))}
    </span>
  );
}
