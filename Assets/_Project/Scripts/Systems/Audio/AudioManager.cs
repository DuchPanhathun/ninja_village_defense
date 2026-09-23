using System.Collections.Generic;
using NinjaVillage.Core.Audio;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinjaVillage.Systems.Audio
{
    /// <summary>
    /// Plays every sound in the game (EPIC 18). Gameplay/UI never touch AudioSources: they raise
    /// <see cref="SfxRequestEvent"/> / <see cref="MusicRequestEvent"/> through <c>Sfx.Play</c>,
    /// and this persistent service resolves the cue in the <see cref="AudioLibrary"/> (or
    /// synthesizes a placeholder), throttles spam, and mixes with the player's volume settings.
    ///
    /// SFX: a fixed pool of voices (oldest voice is stolen when all are busy), per-cue cooldown
    /// and voice cap (<see cref="SfxThrottle"/>), random pitch, cheap 2D stereo pan + off-screen
    /// fade for positional cues. Music: two sources cross-fading on unscaled time, so fades keep
    /// running while the game is paused (timeScale 0).
    ///
    /// Created automatically before the first scene (DontDestroyOnLoad) and listens with
    /// <c>SubscribePersistent</c>, so scene changes (which clear normal subscriptions) don't
    /// silence it. <see cref="MusicDirector"/> and <see cref="GameplaySfxBridge"/> live on the
    /// same GameObject.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField, Min(4)] private int sfxVoiceCount = 16;
        [Tooltip("How much horizontal screen position pans positional sounds (0 = mono).")]
        [SerializeField, Range(0f, 1f)] private float stereoPanStrength = 0.6f;
        [Tooltip("Viewport distance beyond the screen edge at which positional sounds fade to silence.")]
        [SerializeField, Min(0.05f)] private float offscreenFalloff = 0.35f;
        [SerializeField] private float defaultMusicFadeSeconds = 1f;

        public static AudioManager Instance { get; private set; }

        public string CurrentMusicTrack => _currentTrack;
        public AudioLibrary Library => _library;

        /// <summary>Everything the manager needs per cue, resolved once and cached.</summary>
        private sealed class ResolvedCue
        {
            public string Id;
            public CueSettings Settings;
            public readonly List<AudioClip> Clips = new();
        }

        private sealed class MusicChannel
        {
            public AudioSource Source;
            public string TrackId;
            public float Fade;
            public float Target;
            public float Rate;
            public float TrackVolume = 1f;
        }

        private readonly Dictionary<string, ResolvedCue> _resolved = new();
        private readonly SfxThrottle _throttle = new();

        private AudioLibrary _library;
        private AudioSource[] _voices;
        private string[] _voiceCue;
        private float[] _voiceStartTime;

        private MusicChannel _musicA;
        private MusicChannel _musicB;
        private MusicChannel _activeMusic;
        private string _currentTrack;

        private float _masterVolume = 1f;
        private float _musicVolume = 0.8f;
        private float _sfxVolume = 1f;

        private AudioListener _fallbackListener;

        // ------------------------------------------------------------------ lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _library = CatalogLoader.Load<AudioLibrary>();
            CreateSources();
            ReadVolumes(SaveService.Data.Settings);
        }

        private void OnEnable()
        {
            EventBus<SfxRequestEvent>.SubscribePersistent(OnSfxRequested);
            EventBus<MusicRequestEvent>.SubscribePersistent(OnMusicRequested);
            EventBus<SettingsChangedEvent>.SubscribePersistent(OnSettingsChanged);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            EventBus<SfxRequestEvent>.UnsubscribePersistent(OnSfxRequested);
            EventBus<MusicRequestEvent>.UnsubscribePersistent(OnMusicRequested);
            EventBus<SettingsChangedEvent>.UnsubscribePersistent(OnSettingsChanged);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start() => EnsureListener();

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            TickMusic(_musicA, dt);
            TickMusic(_musicB, dt);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[AudioManager]");
            go.AddComponent<AudioManager>();
            go.AddComponent<MusicDirector>();
            go.AddComponent<GameplaySfxBridge>();
        }

        private void CreateSources()
        {
            _voices = new AudioSource[sfxVoiceCount];
            _voiceCue = new string[sfxVoiceCount];
            _voiceStartTime = new float[sfxVoiceCount];
            for (int i = 0; i < sfxVoiceCount; i++)
                _voices[i] = CreateSource($"SFX Voice {i}", false);

            _musicA = new MusicChannel { Source = CreateSource("Music A", true) };
            _musicB = new MusicChannel { Source = CreateSource("Music B", true) };
            _activeMusic = _musicA;
        }

        private AudioSource CreateSource(string sourceName, bool music)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D game: pan is computed manually from screen position
            source.loop = music;
            source.priority = music ? 0 : 128;
            return source;
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Plays a one-shot cue. Prefer <c>Sfx.Play</c>/<c>Sfx.PlayAt</c> from gameplay code.</summary>
        public void PlaySfx(string cueId, Vector2 position, bool positional, float volumeScale = 1f)
        {
            if (string.IsNullOrEmpty(cueId)) return;
            float mix = _masterVolume * _sfxVolume * volumeScale;
            if (mix <= 0.0001f) return;

            var cue = Resolve(cueId);
            if (cue.Clips.Count == 0) return;

            float pan = 0f;
            if (positional && !ComputePositional(position, out pan, out float attenuation))
                return;
            else if (positional)
                mix *= attenuation;

            float now = Time.unscaledTime;
            if (!_throttle.TryPlay(cueId, now, cue.Settings.Cooldown, CountActiveVoices(cueId), cue.Settings.MaxVoices))
                return;

            int voice = AcquireVoice();
            var source = _voices[voice];
            source.Stop();
            source.clip = cue.Clips.Count == 1 ? cue.Clips[0] : cue.Clips[Random.Range(0, cue.Clips.Count)];
            source.volume = Mathf.Clamp01(cue.Settings.Volume * mix);
            source.pitch = Random.Range(cue.Settings.PitchMin, cue.Settings.PitchMax);
            source.panStereo = pan;
            source.Play();

            _voiceCue[voice] = cueId;
            _voiceStartTime[voice] = now;
        }

        /// <summary>Cross-fades to <paramref name="trackId"/> (null/empty = fade out). Same track = no-op.</summary>
        public void PlayMusic(string trackId, float fadeSeconds)
        {
            if (_musicA == null) return;
            if (fadeSeconds < 0f) fadeSeconds = defaultMusicFadeSeconds;
            if (trackId == _currentTrack && (_activeMusic.Source.isPlaying || string.IsNullOrEmpty(trackId))) return;

            _currentTrack = trackId;
            float rate = fadeSeconds > 0.01f ? 1f / fadeSeconds : 1000f;
            var outgoing = _activeMusic;
            var incoming = outgoing == _musicA ? _musicB : _musicA;

            if (string.IsNullOrEmpty(trackId))
            {
                FadeOut(_musicA, rate);
                FadeOut(_musicB, rate);
                return;
            }

            // Returning to the track that is still fading out (boss → battle quickly): just reverse.
            if (incoming.TrackId == trackId && incoming.Source.isPlaying)
            {
                incoming.Target = 1f;
                incoming.Rate = rate;
                FadeOut(outgoing, rate);
                _activeMusic = incoming;
                return;
            }

            var cue = Resolve(trackId);
            if (cue.Clips.Count == 0) return;

            FadeOut(outgoing, rate);

            incoming.Source.Stop();
            incoming.Source.clip = cue.Clips[Random.Range(0, cue.Clips.Count)];
            incoming.Source.loop = cue.Settings.Loop;
            incoming.Source.pitch = 1f;
            incoming.TrackId = trackId;
            incoming.TrackVolume = cue.Settings.Volume;
            incoming.Fade = 0f;
            incoming.Target = 1f;
            incoming.Rate = rate;
            incoming.Source.volume = 0f;
            incoming.Source.Play();
            _activeMusic = incoming;
        }

        public void StopMusic(float fadeSeconds = 0.5f) => PlayMusic(null, fadeSeconds);

        /// <summary>Stops every sound effect immediately (e.g. when leaving a battle).</summary>
        public void StopAllSfx()
        {
            if (_voices == null) return;
            foreach (var voice in _voices)
                if (voice != null) voice.Stop();
        }

        /// <summary>Warms the cache (synthesizes placeholders) so the first play of a cue doesn't hitch.</summary>
        public void Preload(string cueId) => Resolve(cueId);

        // ------------------------------------------------------------------ internals

        private ResolvedCue Resolve(string cueId)
        {
            if (_resolved.TryGetValue(cueId, out var cached)) return cached;

            var resolved = new ResolvedCue { Id = cueId, Settings = AudioCueDefaults.For(cueId) };
            if (_library != null && _library.TryGet(cueId, out var entry))
            {
                resolved.Settings = entry.Settings;
                foreach (var clip in entry.Clips)
                    if (clip != null) resolved.Clips.Add(clip);
            }

            if (resolved.Clips.Count == 0)
                resolved.Clips.Add(ProceduralAudio.CreateClip(cueId)); // placeholder until real audio exists

            _resolved[cueId] = resolved;
            return resolved;
        }

        private bool ComputePositional(Vector2 position, out float pan, out float attenuation)
        {
            pan = 0f;
            attenuation = 1f;
            var cam = Camera.main;
            if (cam == null) return true;

            Vector3 viewport = cam.WorldToViewportPoint(position);
            pan = Mathf.Clamp((viewport.x - 0.5f) * 2f, -1f, 1f) * stereoPanStrength;

            float outside = Mathf.Max(Mathf.Max(-viewport.x, viewport.x - 1f), Mathf.Max(-viewport.y, viewport.y - 1f));
            if (outside > 0f)
            {
                attenuation = 1f - outside / offscreenFalloff;
                if (attenuation <= 0.02f) return false;
            }
            return true;
        }

        private int CountActiveVoices(string cueId)
        {
            int count = 0;
            for (int i = 0; i < _voices.Length; i++)
                if (_voiceCue[i] == cueId && _voices[i].isPlaying) count++;
            return count;
        }

        private int AcquireVoice()
        {
            int oldest = 0;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (!_voices[i].isPlaying) return i;
                if (_voiceStartTime[i] < oldestTime)
                {
                    oldestTime = _voiceStartTime[i];
                    oldest = i;
                }
            }
            return oldest; // voice limit reached: steal the oldest
        }

        private static void FadeOut(MusicChannel channel, float rate)
        {
            channel.Target = 0f;
            channel.Rate = rate;
        }

        private void TickMusic(MusicChannel channel, float dt)
        {
            if (channel == null || channel.Source == null) return;
            if (!channel.Source.isPlaying && channel.Target <= 0f) return;

            channel.Fade = Mathf.MoveTowards(channel.Fade, channel.Target, channel.Rate * dt);
            channel.Source.volume = channel.Fade * channel.TrackVolume * _musicVolume * _masterVolume;

            if (channel.Fade <= 0f && channel.Target <= 0f && channel.Source.isPlaying)
            {
                channel.Source.Stop();
                channel.TrackId = null;
            }
        }

        private void ReadVolumes(SettingsSaveData settings)
        {
            if (settings == null) return;
            _masterVolume = SettingsSaveData.ClampVolume(settings.MasterVolume);
            _musicVolume = SettingsSaveData.ClampVolume(settings.MusicVolume);
            _sfxVolume = SettingsSaveData.ClampVolume(settings.SfxVolume);
        }

        /// <summary>
        /// Every scene should have an AudioListener (usually on the camera). If one doesn't, a
        /// fallback on this object is enabled so audio never goes silent; it is disabled again
        /// as soon as a scene brings its own, avoiding the "2 audio listeners" warning.
        /// </summary>
        private void EnsureListener()
        {
            bool sceneHasListener = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener != _fallbackListener && listener.isActiveAndEnabled)
                {
                    sceneHasListener = true;
                    break;
                }
            }

            if (sceneHasListener)
            {
                if (_fallbackListener != null) _fallbackListener.enabled = false;
                return;
            }

            if (_fallbackListener == null) _fallbackListener = gameObject.AddComponent<AudioListener>();
            _fallbackListener.enabled = true;
        }

        // ------------------------------------------------------------------ event handlers

        private void OnSfxRequested(SfxRequestEvent evt) => PlaySfx(evt.CueId, evt.Position, evt.Positional, evt.VolumeScale);

        private void OnMusicRequested(MusicRequestEvent evt) => PlayMusic(evt.TrackId, evt.FadeSeconds);

        private void OnSettingsChanged(SettingsChangedEvent evt) => ReadVolumes(evt.Settings);

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            EnsureListener();
            // One-shots from the previous scene (a death scream mid-transition) shouldn't bleed over.
            StopAllSfx();
        }
    }
}
