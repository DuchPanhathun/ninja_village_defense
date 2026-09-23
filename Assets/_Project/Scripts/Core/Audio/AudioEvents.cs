using NinjaVillage.Core.Events;
using UnityEngine;

namespace NinjaVillage.Core.Audio
{
    /// <summary>Asks the audio system to play a one-shot sound. Raise via <see cref="Sfx"/>.</summary>
    public readonly struct SfxRequestEvent : IGameEvent
    {
        public readonly string CueId;
        public readonly Vector2 Position;
        /// <summary>False = 2D/UI sound (position ignored).</summary>
        public readonly bool Positional;
        public readonly float VolumeScale;

        public SfxRequestEvent(string cueId, Vector2 position, bool positional, float volumeScale = 1f)
        {
            CueId = cueId;
            Position = position;
            Positional = positional;
            VolumeScale = volumeScale;
        }
    }

    /// <summary>Asks the audio system to cross-fade to a music track. Null/empty id = stop music.</summary>
    public readonly struct MusicRequestEvent : IGameEvent
    {
        public readonly string TrackId;
        public readonly float FadeSeconds;

        public MusicRequestEvent(string trackId, float fadeSeconds = 1f)
        {
            TrackId = trackId;
            FadeSeconds = fadeSeconds;
        }
    }

    /// <summary>
    /// One-liners for gameplay/UI code: <c>Sfx.Play(AudioCueIds.UiClick)</c>. Callers never
    /// reference the AudioManager, so audio can be absent (tests, early scenes) without errors.
    /// </summary>
    public static class Sfx
    {
        public static void Play(string cueId, float volumeScale = 1f) =>
            EventBus<SfxRequestEvent>.Raise(new SfxRequestEvent(cueId, Vector2.zero, false, volumeScale));

        public static void PlayAt(string cueId, Vector2 position, float volumeScale = 1f) =>
            EventBus<SfxRequestEvent>.Raise(new SfxRequestEvent(cueId, position, true, volumeScale));

        public static void Music(string trackId, float fadeSeconds = 1f) =>
            EventBus<MusicRequestEvent>.Raise(new MusicRequestEvent(trackId, fadeSeconds));
    }

    /// <summary>Stable cue ids. The AudioLibrary asset maps each id to one or more clips.</summary>
    public static class AudioCueIds
    {
        // Weapons
        public const string KunaiThrow = "sfx_kunai_throw";
        public const string ShurikenThrow = "sfx_shuriken_throw";
        public const string KatanaSlash = "sfx_katana_slash";
        public const string BowShot = "sfx_bow_shot";
        public const string BowChargedShot = "sfx_bow_charged";
        public const string ChainSickleSwing = "sfx_chain_sickle";

        // Combat
        public const string EnemyHit = "sfx_enemy_hit";
        public const string CriticalHit = "sfx_critical_hit";
        public const string EnemyDeath = "sfx_enemy_death";
        public const string PlayerHurt = "sfx_player_hurt";
        public const string PlayerDeath = "sfx_player_death";
        public const string BossRoar = "sfx_boss_roar";
        public const string Dash = "sfx_dash";

        // Skills / ultimates
        public const string LightningStrike = "sfx_lightning";
        public const string Explosion = "sfx_explosion";
        public const string Burn = "sfx_burn";
        public const string Poison = "sfx_poison";
        public const string Shield = "sfx_shield";
        public const string SmokeBomb = "sfx_smoke_bomb";
        public const string Teleport = "sfx_teleport";
        public const string UltimateActivate = "sfx_ultimate";
        public const string EvolutionUnlock = "sfx_evolution";

        // Pickups / progression
        public const string CoinPickup = "sfx_coin";
        public const string XpPickup = "sfx_xp";
        public const string EquipmentPickup = "sfx_equipment";
        public const string ChestOpen = "sfx_chest";
        public const string LevelUp = "sfx_level_up";
        public const string SkillSelect = "sfx_skill_select";

        // UI
        public const string UiClick = "sfx_ui_click";
        public const string UiBack = "sfx_ui_back";
        public const string UiPurchase = "sfx_ui_purchase";
        public const string UiError = "sfx_ui_error";
        public const string UiUpgrade = "sfx_ui_upgrade";
        public const string RewardClaim = "sfx_reward_claim";

        // Music tracks
        public const string MusicMenu = "music_menu";
        public const string MusicVillage = "music_village";
        public const string MusicBattle = "music_battle";
        public const string MusicBoss = "music_boss";
        public const string MusicVictory = "music_victory";
        public const string MusicDefeat = "music_defeat";
    }
}
