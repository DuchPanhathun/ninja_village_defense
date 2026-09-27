using System;
using System.Collections.Generic;
using System.Reflection;
using NinjaVillage.Core.Audio;

namespace NinjaVillage.Systems.Audio
{
    /// <summary>Playback rules for one cue: loudness, random pitch range, spam limits, looping.</summary>
    public readonly struct CueSettings
    {
        public readonly float Volume;
        public readonly float PitchMin;
        public readonly float PitchMax;
        /// <summary>Minimum seconds between two plays of the same cue (drops the extra requests).</summary>
        public readonly float Cooldown;
        /// <summary>Max simultaneous voices of this cue; 0 = unlimited (bounded by the pool).</summary>
        public readonly int MaxVoices;
        public readonly bool Loop;

        public CueSettings(float volume, float pitchMin, float pitchMax, float cooldown, int maxVoices, bool loop = false)
        {
            Volume = volume;
            PitchMin = pitchMin;
            PitchMax = pitchMax;
            Cooldown = cooldown;
            MaxVoices = maxVoices;
            Loop = loop;
        }
    }

    /// <summary>
    /// Sensible per-cue defaults, used both when the AudioLibrary asset has no entry for a cue
    /// and by the content generator to seed new entries. Hits/deaths get short cooldowns and
    /// low voice caps so 50 simultaneous kills sound like a crunch, not a wall of noise; UI
    /// cues keep a steady pitch; music loops except the victory/defeat stingers.
    /// </summary>
    public static class AudioCueDefaults
    {
        public const string MusicPrefix = "music_";

        private static List<string> _allCueIds;

        public static bool IsMusic(string cueId) =>
            cueId != null && cueId.StartsWith(MusicPrefix, StringComparison.Ordinal);

        public static CueSettings For(string cueId)
        {
            switch (cueId)
            {
                // Weapons — fire constantly, keep them light.
                case AudioCueIds.KunaiThrow:
                case AudioCueIds.ShurikenThrow:
                case AudioCueIds.BowShot:
                    return new CueSettings(0.35f, 0.92f, 1.08f, 0.06f, 3);
                case AudioCueIds.KatanaSlash:
                case AudioCueIds.ChainSickleSwing:
                    return new CueSettings(0.45f, 0.92f, 1.08f, 0.08f, 3);
                case AudioCueIds.BowChargedShot:
                    return new CueSettings(0.6f, 0.95f, 1.05f, 0.15f, 2);

                // Combat
                case AudioCueIds.EnemyHit:
                    return new CueSettings(0.3f, 0.88f, 1.12f, 0.045f, 4);
                case AudioCueIds.CriticalHit:
                    return new CueSettings(0.45f, 0.95f, 1.08f, 0.07f, 3);
                case AudioCueIds.EnemyDeath:
                    return new CueSettings(0.4f, 0.85f, 1.15f, 0.06f, 4);
                case AudioCueIds.PlayerHurt:
                    return new CueSettings(0.7f, 0.95f, 1.05f, 0.2f, 1);
                case AudioCueIds.PlayerDeath:
                    return new CueSettings(0.9f, 1f, 1f, 1f, 1);
                case AudioCueIds.BossRoar:
                    return new CueSettings(1f, 0.95f, 1.02f, 2f, 1);
                case AudioCueIds.Dash:
                    return new CueSettings(0.5f, 0.95f, 1.05f, 0.1f, 2);

                // Skills / ultimates
                case AudioCueIds.LightningStrike:
                    return new CueSettings(0.55f, 0.9f, 1.1f, 0.06f, 3);
                case AudioCueIds.Explosion:
                    return new CueSettings(0.65f, 0.9f, 1.1f, 0.08f, 3);
                case AudioCueIds.Burn:
                case AudioCueIds.Poison:
                    return new CueSettings(0.3f, 0.9f, 1.1f, 0.15f, 2);
                case AudioCueIds.Shield:
                case AudioCueIds.SmokeBomb:
                    return new CueSettings(0.55f, 0.97f, 1.03f, 0.3f, 1);
                case AudioCueIds.Teleport:
                    return new CueSettings(0.55f, 0.95f, 1.05f, 0.2f, 2);
                case AudioCueIds.UltimateActivate:
                case AudioCueIds.EvolutionUnlock:
                    return new CueSettings(0.95f, 1f, 1f, 0.5f, 1);

                // Pickups / progression
                case AudioCueIds.CoinPickup:
                    return new CueSettings(0.35f, 0.97f, 1.08f, 0.05f, 3);
                case AudioCueIds.XpPickup:
                    return new CueSettings(0.22f, 0.95f, 1.12f, 0.04f, 3);
                case AudioCueIds.EquipmentPickup:
                    return new CueSettings(0.6f, 0.98f, 1.02f, 0.1f, 2);
                case AudioCueIds.ChestOpen:
                    return new CueSettings(0.8f, 1f, 1f, 0.2f, 1);
                case AudioCueIds.LevelUp:
                    return new CueSettings(0.75f, 1f, 1f, 0.3f, 1);
                case AudioCueIds.SkillSelect:
                    return new CueSettings(0.65f, 1f, 1f, 0.1f, 1);

                // UI — no pitch wobble, tiny cooldown (double-tap protection only).
                case AudioCueIds.UiClick:
                case AudioCueIds.UiBack:
                    return new CueSettings(0.6f, 1f, 1f, 0.03f, 2);
                case AudioCueIds.UiPurchase:
                case AudioCueIds.UiUpgrade:
                case AudioCueIds.RewardClaim:
                    return new CueSettings(0.75f, 1f, 1f, 0.08f, 2);
                case AudioCueIds.UiError:
                    return new CueSettings(0.6f, 1f, 1f, 0.15f, 1);

                // Music
                case AudioCueIds.MusicVictory:
                case AudioCueIds.MusicDefeat:
                    return new CueSettings(0.7f, 1f, 1f, 0f, 1, loop: false);
            }

            if (IsMusic(cueId)) return new CueSettings(0.55f, 1f, 1f, 0f, 1, loop: true);
            return new CueSettings(0.5f, 0.95f, 1.05f, 0.05f, 3);
        }

        /// <summary>Every <c>public const string</c> in <see cref="AudioCueIds"/> — the generator makes an entry for each.</summary>
        public static IReadOnlyList<string> AllCueIds
        {
            get
            {
                if (_allCueIds != null) return _allCueIds;
                _allCueIds = new List<string>();
                foreach (var field in typeof(AudioCueIds).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.IsLiteral && field.FieldType == typeof(string) && field.GetRawConstantValue() is string id)
                        _allCueIds.Add(id);
                }
                return _allCueIds;
            }
        }
    }
}
