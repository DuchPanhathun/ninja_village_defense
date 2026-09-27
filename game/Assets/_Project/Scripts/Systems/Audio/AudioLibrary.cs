using System;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Audio
{
    /// <summary>One cue id (see <c>AudioCueIds</c>) → clips + playback rules.</summary>
    [Serializable]
    public class AudioCueEntry
    {
        [SerializeField] private string id;
        [Tooltip("One is picked at random per play. Empty = procedural placeholder sound.")]
        [SerializeField] private AudioClip[] clips = new AudioClip[0];
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;
        [SerializeField] private float pitchMin = 1f;
        [SerializeField] private float pitchMax = 1f;
        [Tooltip("Minimum seconds between two plays of this cue; extra requests are dropped.")]
        [SerializeField] private float cooldown = 0.05f;
        [Tooltip("Max voices of this cue sounding at once (0 = no cap).")]
        [SerializeField] private int maxVoices = 3;
        [Tooltip("Music tracks only.")]
        [SerializeField] private bool loop;

        public string Id => id;
        public IReadOnlyList<AudioClip> Clips => clips;
        public bool HasClips
        {
            get
            {
                if (clips == null) return false;
                foreach (var clip in clips)
                    if (clip != null) return true;
                return false;
            }
        }

        public CueSettings Settings => new(volume, Mathf.Min(pitchMin, pitchMax), Mathf.Max(pitchMin, pitchMax), cooldown, maxVoices, loop);

        public AudioCueEntry() { }

        public AudioCueEntry(string id, CueSettings settings, AudioClip[] clips = null)
        {
            this.id = id;
            volume = settings.Volume;
            pitchMin = settings.PitchMin;
            pitchMax = settings.PitchMax;
            cooldown = settings.Cooldown;
            maxVoices = settings.MaxVoices;
            loop = settings.Loop;
            this.clips = clips ?? new AudioClip[0];
        }

        /// <summary>Editor/generator use: fills the clip list when it is still empty.</summary>
        public void SetClips(AudioClip[] newClips) => clips = newClips ?? new AudioClip[0];
    }

    /// <summary>
    /// The game's sound map: every <c>AudioCueIds</c> constant should have an entry (the
    /// content generator creates them). Gameplay never references clips directly — it asks for
    /// a cue id — so replacing placeholder audio with real assets is a data-only change here.
    /// Lives at <c>Resources/Catalogs/AudioLibrary.asset</c> and is loaded by the AudioManager.
    /// </summary>
    [CreateAssetMenu(menuName = "Ninja Village/Audio/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [SerializeField] private List<AudioCueEntry> cues = new();

        private Dictionary<string, AudioCueEntry> _byId;

        public IReadOnlyList<AudioCueEntry> Cues => cues;

        public bool TryGet(string cueId, out AudioCueEntry entry)
        {
            entry = null;
            if (string.IsNullOrEmpty(cueId)) return false;
            if (_byId == null)
            {
                _byId = new Dictionary<string, AudioCueEntry>();
                foreach (var cue in cues)
                {
                    if (cue == null || string.IsNullOrEmpty(cue.Id)) continue;
                    if (!_byId.TryAdd(cue.Id, cue))
                        Debug.LogWarning($"AudioLibrary: duplicate cue id '{cue.Id}'.", this);
                }
            }
            return _byId.TryGetValue(cueId, out entry);
        }

        private void OnEnable() => _byId = null;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the content generator.</summary>
        public void EditorSetCues(IEnumerable<AudioCueEntry> newCues)
        {
            cues = new List<AudioCueEntry>(newCues);
            _byId = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
