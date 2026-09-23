using System.Collections.Generic;

namespace NinjaVillage.Systems.Audio
{
    /// <summary>
    /// Decides whether a sound request may play: enforces the per-cue cooldown and voice cap.
    /// This is what keeps 50 enemies dying in one frame from playing 50 death sounds, and it
    /// also de-duplicates the same cue requested twice in a frame by two systems (e.g. a pickup
    /// script and the gameplay SFX bridge). Plain C# so the rules are unit-testable.
    /// </summary>
    public sealed class SfxThrottle
    {
        private readonly Dictionary<string, float> _lastPlayTime = new();

        /// <summary>
        /// Returns true (and records the play) if <paramref name="cueId"/> may play at <paramref name="now"/>.
        /// <paramref name="activeVoices"/> = voices of this cue still sounding; <paramref name="maxVoices"/> 0 = no cap.
        /// </summary>
        public bool TryPlay(string cueId, float now, float cooldown, int activeVoices, int maxVoices)
        {
            if (string.IsNullOrEmpty(cueId)) return false;
            if (maxVoices > 0 && activeVoices >= maxVoices) return false;

            if (cooldown > 0f && _lastPlayTime.TryGetValue(cueId, out float last) && now >= last && now - last < cooldown)
                return false;

            _lastPlayTime[cueId] = now;
            return true;
        }

        /// <summary>Seconds until the cue is allowed again (0 = ready).</summary>
        public float CooldownRemaining(string cueId, float now, float cooldown)
        {
            if (string.IsNullOrEmpty(cueId) || !_lastPlayTime.TryGetValue(cueId, out float last)) return 0f;
            float remaining = cooldown - (now - last);
            return remaining > 0f ? remaining : 0f;
        }

        public void Reset() => _lastPlayTime.Clear();
    }
}
