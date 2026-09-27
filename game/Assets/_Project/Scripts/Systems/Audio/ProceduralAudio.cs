using System;
using NinjaVillage.Core.Audio;
using UnityEngine;

namespace NinjaVillage.Systems.Audio
{
    /// <summary>
    /// PLACEHOLDER AUDIO. The project has no sound files yet, so every cue without a clip in the
    /// AudioLibrary is synthesized here from simple oscillators and filtered noise — whooshes for
    /// throws, thumps for hits, arpeggios for rewards, and short procedural music loops per
    /// track. It gives real, distinguishable feedback today; drop real clips into the
    /// AudioLibrary asset and they win automatically.
    ///
    /// <see cref="Synthesize"/> is pure C# (deterministic per cue id) so it is unit-tested;
    /// <see cref="CreateClip"/> wraps it in an AudioClip.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 22050;

        public static AudioClip CreateClip(string cueId)
        {
            float[] samples = Synthesize(cueId, SampleRate);
            var clip = AudioClip.Create($"placeholder_{cueId}", samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Mono samples in [-1, 1] for <paramref name="cueId"/>. Never empty.</summary>
        public static float[] Synthesize(string cueId, int sampleRate = SampleRate)
        {
            cueId ??= string.Empty;
            uint seed = StableHash(cueId);

            if (AudioCueDefaults.IsMusic(cueId))
                return Music.Compose(cueId, sampleRate, seed);

            Synth s;
            switch (cueId)
            {
                // ---------------------------------------------------------------- weapons
                case AudioCueIds.KunaiThrow:
                    s = new Synth(sampleRate, 0.14f, seed);
                    s.Noise(0f, 0.12f, 0.7f, 0.015f, 22f, 0.35f, 0.12f, 0.02f);
                    s.Tone(0f, 0.08f, 1800f, 900f, Wave.Sine, 0.15f, 0.003f, 30f);
                    break;
                case AudioCueIds.ShurikenThrow:
                    s = new Synth(sampleRate, 0.18f, seed);
                    s.Noise(0f, 0.14f, 0.6f, 0.01f, 20f, 0.5f, 0.2f, 0.05f);
                    s.Tone(0f, 0.16f, 2600f, 2400f, Wave.Sine, 0.12f, 0.003f, 22f);
                    s.Tone(0f, 0.12f, 3900f, 3700f, Wave.Sine, 0.06f, 0.003f, 28f);
                    break;
                case AudioCueIds.KatanaSlash:
                    s = new Synth(sampleRate, 0.26f, seed);
                    s.Noise(0f, 0.22f, 0.8f, 0.03f, 12f, 0.15f, 0.6f, 0.05f);
                    s.Tone(0.05f, 0.2f, 1500f, 1400f, Wave.Triangle, 0.1f, 0.005f, 15f);
                    break;
                case AudioCueIds.BowShot:
                    s = new Synth(sampleRate, 0.18f, seed);
                    s.Tone(0f, 0.16f, 220f, 180f, Wave.Triangle, 0.55f, 0.002f, 24f);
                    s.Noise(0f, 0.05f, 0.3f, 0.002f, 60f, 0.6f, 0.6f, 0f);
                    break;
                case AudioCueIds.BowChargedShot:
                    s = new Synth(sampleRate, 0.34f, seed);
                    s.Tone(0f, 0.3f, 180f, 140f, Wave.Saw, 0.35f, 0.002f, 12f);
                    s.Noise(0f, 0.25f, 0.4f, 0.005f, 10f, 0.3f, 0.3f, 0f);
                    s.Tone(0f, 0.2f, 880f, 1760f, Wave.Sine, 0.15f, 0.01f, 10f);
                    break;
                case AudioCueIds.ChainSickleSwing:
                    s = new Synth(sampleRate, 0.24f, seed);
                    s.Noise(0f, 0.2f, 0.6f, 0.02f, 14f, 0.25f, 0.25f, 0.03f);
                    s.Tone(0.02f, 0.05f, 3200f, 3200f, Wave.Square, 0.07f, 0.001f, 60f);
                    s.Tone(0.07f, 0.05f, 2800f, 2800f, Wave.Square, 0.07f, 0.001f, 60f);
                    s.Tone(0.12f, 0.05f, 3000f, 3000f, Wave.Square, 0.06f, 0.001f, 60f);
                    break;

                // ---------------------------------------------------------------- combat
                case AudioCueIds.EnemyHit:
                    s = new Synth(sampleRate, 0.09f, seed);
                    s.Noise(0f, 0.07f, 0.6f, 0.001f, 45f, 0.5f, 0.5f, 0f);
                    s.Tone(0f, 0.08f, 180f, 90f, Wave.Sine, 0.5f, 0.001f, 35f);
                    break;
                case AudioCueIds.CriticalHit:
                    s = new Synth(sampleRate, 0.2f, seed);
                    s.Noise(0f, 0.09f, 0.6f, 0.001f, 40f, 0.8f, 0.8f, 0.05f);
                    s.Tone(0f, 0.09f, 200f, 80f, Wave.Sine, 0.5f, 0.001f, 30f);
                    s.Tone(0.01f, 0.18f, 1760f, 1760f, Wave.Sine, 0.25f, 0.002f, 16f);
                    s.Tone(0.01f, 0.18f, 2637f, 2637f, Wave.Sine, 0.12f, 0.002f, 18f);
                    break;
                case AudioCueIds.EnemyDeath:
                    s = new Synth(sampleRate, 0.28f, seed);
                    s.Tone(0f, 0.25f, 440f, 110f, Wave.Square, 0.22f, 0.002f, 8f);
                    s.Noise(0f, 0.2f, 0.35f, 0.002f, 14f, 0.3f, 0.15f, 0f);
                    break;
                case AudioCueIds.PlayerHurt:
                    s = new Synth(sampleRate, 0.22f, seed);
                    s.Tone(0f, 0.2f, 330f, 165f, Wave.Square, 0.3f, 0.002f, 10f);
                    s.Noise(0f, 0.1f, 0.3f, 0.001f, 30f, 0.4f, 0.4f, 0f);
                    break;
                case AudioCueIds.PlayerDeath:
                    s = new Synth(sampleRate, 1.1f, seed);
                    s.Tone(0f, 1.05f, 440f, 55f, Wave.Saw, 0.3f, 0.01f, 2.5f, 6f, 0.03f);
                    s.Tone(0f, 1.05f, 220f, 40f, Wave.Square, 0.15f, 0.01f, 2.5f);
                    s.Noise(0f, 0.6f, 0.3f, 0.005f, 4f, 0.2f, 0.05f, 0f);
                    break;
                case AudioCueIds.BossRoar:
                    s = new Synth(sampleRate, 1.35f, seed);
                    s.Tone(0f, 1.3f, 70f, 55f, Wave.Saw, 0.45f, 0.15f, 1.5f, 7f, 0.06f);
                    s.Tone(0f, 1.3f, 105f, 80f, Wave.Saw, 0.3f, 0.15f, 1.5f, 5f, 0.05f);
                    s.Noise(0f, 1.2f, 0.5f, 0.1f, 2f, 0.12f, 0.08f, 0f);
                    break;
                case AudioCueIds.Dash:
                    s = new Synth(sampleRate, 0.17f, seed);
                    s.Noise(0f, 0.15f, 0.6f, 0.01f, 15f, 0.15f, 0.5f, 0.03f);
                    s.Tone(0f, 0.12f, 300f, 900f, Wave.Sine, 0.15f, 0.005f, 18f);
                    break;

                // ---------------------------------------------------------------- skills / ultimates
                case AudioCueIds.LightningStrike:
                    s = new Synth(sampleRate, 0.45f, seed);
                    for (int i = 0; i < 4; i++)
                        s.Noise(i * 0.05f, 0.06f, 0.8f, 0.001f, 40f, 0.9f, 0.9f, 0.2f);
                    s.Noise(0f, 0.4f, 0.35f, 0.01f, 6f, 0.15f, 0.05f, 0f);
                    s.Tone(0f, 0.3f, 120f, 50f, Wave.Sine, 0.3f, 0.002f, 8f);
                    break;
                case AudioCueIds.Explosion:
                    s = new Synth(sampleRate, 0.85f, seed);
                    s.Noise(0f, 0.8f, 1f, 0.004f, 5f, 0.3f, 0.04f, 0f);
                    s.Tone(0f, 0.5f, 90f, 35f, Wave.Sine, 0.7f, 0.002f, 7f);
                    break;
                case AudioCueIds.Burn:
                    s = new Synth(sampleRate, 0.4f, seed);
                    s.Noise(0f, 0.38f, 0.25f, 0.02f, 5f, 0.5f, 0.3f, 0.1f);
                    for (int i = 0; i < 6; i++)
                        s.Noise(s.RandomRange(0f, 0.32f), 0.03f, 0.5f, 0.001f, 80f, 0.9f, 0.9f, 0.3f);
                    break;
                case AudioCueIds.Poison:
                    s = new Synth(sampleRate, 0.38f, seed);
                    for (int i = 0; i < 5; i++)
                    {
                        float f = s.RandomRange(280f, 560f);
                        s.Tone(i * 0.065f, 0.07f, f, f * 1.6f, Wave.Sine, 0.3f, 0.004f, 25f);
                    }
                    break;
                case AudioCueIds.Shield:
                    s = new Synth(sampleRate, 0.42f, seed);
                    s.Tone(0f, 0.4f, 440f, 880f, Wave.Triangle, 0.3f, 0.05f, 4f);
                    s.Tone(0f, 0.4f, 660f, 1320f, Wave.Sine, 0.2f, 0.05f, 4f);
                    s.Tone(0.05f, 0.35f, 1320f, 1320f, Wave.Sine, 0.08f, 0.05f, 5f, 12f, 0.02f);
                    break;
                case AudioCueIds.SmokeBomb:
                    s = new Synth(sampleRate, 0.65f, seed);
                    s.Tone(0f, 0.06f, 600f, 200f, Wave.Sine, 0.3f, 0.001f, 30f);
                    s.Noise(0f, 0.6f, 0.7f, 0.01f, 4f, 0.12f, 0.03f, 0f);
                    break;
                case AudioCueIds.Teleport:
                    s = new Synth(sampleRate, 0.3f, seed);
                    s.Tone(0f, 0.25f, 300f, 2400f, Wave.Sine, 0.3f, 0.005f, 5f);
                    s.Tone(0f, 0.25f, 450f, 3600f, Wave.Triangle, 0.15f, 0.005f, 5f);
                    break;
                case AudioCueIds.UltimateActivate:
                    s = new Synth(sampleRate, 1.2f, seed);
                    s.Noise(0f, 0.8f, 0.3f, 0.6f, 1f, 0.1f, 0.6f, 0f);
                    foreach (float f in new[] { 130.8f, 196f, 261.6f, 329.6f })
                        s.Tone(0.15f, 1f, f, f, Wave.Saw, 0.12f, 0.2f, 2f, 5f, 0.004f);
                    s.Tone(0.2f, 0.6f, 130f, 65f, Wave.Sine, 0.5f, 0.005f, 5f);
                    break;
                case AudioCueIds.EvolutionUnlock:
                    s = new Synth(sampleRate, 1f, seed);
                    Arpeggio(s, 0f, 0.1f, 0.5f, Wave.Triangle, 0.3f, 523.3f, 659.3f, 784f, 1046.5f, 1318.5f);
                    s.Tone(0.5f, 0.45f, 2093f, 2093f, Wave.Sine, 0.08f, 0.02f, 4f, 10f, 0.01f);
                    break;

                // ---------------------------------------------------------------- pickups / progression
                case AudioCueIds.CoinPickup:
                    s = new Synth(sampleRate, 0.18f, seed);
                    s.Tone(0f, 0.05f, 988f, 988f, Wave.Square, 0.18f, 0.001f, 25f);
                    s.Tone(0.05f, 0.12f, 1318.5f, 1318.5f, Wave.Square, 0.18f, 0.001f, 18f);
                    break;
                case AudioCueIds.XpPickup:
                    s = new Synth(sampleRate, 0.08f, seed);
                    s.Tone(0f, 0.07f, 1200f, 1800f, Wave.Sine, 0.4f, 0.002f, 30f);
                    break;
                case AudioCueIds.EquipmentPickup:
                    s = new Synth(sampleRate, 0.4f, seed);
                    Arpeggio(s, 0f, 0.07f, 0.2f, Wave.Triangle, 0.3f, 659.3f, 880f, 1318.5f);
                    break;
                case AudioCueIds.ChestOpen:
                    s = new Synth(sampleRate, 0.75f, seed);
                    s.Noise(0f, 0.15f, 0.3f, 0.02f, 10f, 0.1f, 0.1f, 0f);
                    Arpeggio(s, 0.1f, 0.07f, 0.3f, Wave.Square, 0.13f, 523.3f, 659.3f, 784f, 1046.5f, 1318.5f);
                    break;
                case AudioCueIds.LevelUp:
                    s = new Synth(sampleRate, 0.75f, seed);
                    Arpeggio(s, 0f, 0.08f, 0.25f, Wave.Square, 0.12f, 523.3f, 659.3f, 784f);
                    s.Tone(0.24f, 0.5f, 1046.5f, 1046.5f, Wave.Triangle, 0.35f, 0.005f, 4f, 6f, 0.01f);
                    s.Tone(0.24f, 0.5f, 1318.5f, 1318.5f, Wave.Sine, 0.15f, 0.005f, 4f);
                    break;
                case AudioCueIds.SkillSelect:
                    s = new Synth(sampleRate, 0.26f, seed);
                    s.Tone(0f, 0.09f, 784f, 784f, Wave.Triangle, 0.35f, 0.002f, 15f);
                    s.Tone(0.08f, 0.17f, 1174.7f, 1174.7f, Wave.Triangle, 0.35f, 0.002f, 10f);
                    break;

                // ---------------------------------------------------------------- UI
                case AudioCueIds.UiClick:
                    s = new Synth(sampleRate, 0.045f, seed);
                    s.Tone(0f, 0.035f, 1500f, 1100f, Wave.Square, 0.15f, 0.001f, 60f);
                    s.Noise(0f, 0.01f, 0.15f, 0.0005f, 200f, 1f, 1f, 0.5f);
                    break;
                case AudioCueIds.UiBack:
                    s = new Synth(sampleRate, 0.05f, seed);
                    s.Tone(0f, 0.045f, 900f, 600f, Wave.Square, 0.15f, 0.001f, 50f);
                    break;
                case AudioCueIds.UiPurchase:
                    s = new Synth(sampleRate, 0.4f, seed);
                    s.Tone(0f, 0.06f, 1318.5f, 1318.5f, Wave.Square, 0.15f, 0.001f, 25f);
                    s.Tone(0.06f, 0.3f, 1760f, 1760f, Wave.Square, 0.15f, 0.001f, 10f);
                    s.Noise(0.06f, 0.25f, 0.12f, 0.001f, 15f, 1f, 1f, 0.4f);
                    break;
                case AudioCueIds.UiError:
                    s = new Synth(sampleRate, 0.28f, seed);
                    s.Tone(0f, 0.1f, 180f, 180f, Wave.Square, 0.25f, 0.002f, 3f);
                    s.Tone(0.13f, 0.12f, 150f, 150f, Wave.Square, 0.25f, 0.002f, 3f);
                    break;
                case AudioCueIds.UiUpgrade:
                    s = new Synth(sampleRate, 0.45f, seed);
                    Arpeggio(s, 0f, 0.07f, 0.22f, Wave.Triangle, 0.35f, 523.3f, 784f, 1046.5f);
                    s.Noise(0.14f, 0.25f, 0.08f, 0.01f, 12f, 1f, 1f, 0.5f);
                    break;
                case AudioCueIds.RewardClaim:
                    s = new Synth(sampleRate, 0.6f, seed);
                    Arpeggio(s, 0f, 0.06f, 0.3f, Wave.Square, 0.12f, 784f, 987.8f, 1174.7f, 1568f);
                    s.Noise(0.2f, 0.35f, 0.1f, 0.01f, 8f, 1f, 1f, 0.5f);
                    break;

                // ---------------------------------------------------------------- unknown ids
                default:
                    s = new Synth(sampleRate, 0.12f, seed);
                    float pitch = 400f + (seed % 800u);
                    s.Tone(0f, 0.1f, pitch, pitch * 1.25f, Wave.Triangle, 0.35f, 0.002f, 20f);
                    break;
            }

            return s.Finish(0.9f);
        }

        /// <summary>FNV-1a — stable across runtimes, unlike string.GetHashCode.</summary>
        public static uint StableHash(string text)
        {
            uint hash = 2166136261u;
            if (text == null) return hash;
            foreach (char c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }
            return hash;
        }

        private static void Arpeggio(Synth s, float start, float step, float noteLength, Wave wave, float amp, params float[] frequencies)
        {
            for (int i = 0; i < frequencies.Length; i++)
                s.Tone(start + i * step, noteLength, frequencies[i], frequencies[i], wave, amp, 0.002f, 8f);
        }

        internal static float MidiToHz(int midi) => (float)(440.0 * Math.Pow(2.0, (midi - 69) / 12.0));

        // ==================================================================== synth core

        internal enum Wave { Sine, Square, Saw, Triangle }

        /// <summary>Additive mono buffer with a few oscillator/noise primitives.</summary>
        internal sealed class Synth
        {
            private const double TwoPi = Math.PI * 2.0;

            private readonly float[] _buffer;
            private readonly int _sampleRate;
            private readonly bool _wrap;
            private uint _rng;

            public int Length => _buffer.Length;
            public int SampleRate => _sampleRate;

            /// <param name="wrap">Loops: sound past the end continues at the start, so the loop point is seamless.</param>
            public Synth(int sampleRate, float seconds, uint seed, bool wrap = false)
            {
                _sampleRate = Math.Max(1000, sampleRate);
                _buffer = new float[Math.Max(1, (int)(seconds * _sampleRate))];
                _wrap = wrap;
                _rng = seed == 0 ? 0x9E3779B9u : seed;
            }

            public float RandomValue()
            {
                // xorshift32
                _rng ^= _rng << 13;
                _rng ^= _rng >> 17;
                _rng ^= _rng << 5;
                return (_rng & 0xFFFFFF) / (float)0x1000000;
            }

            public float RandomRange(float min, float max) => min + (max - min) * RandomValue();

            /// <summary>
            /// Oscillator note with exponential pitch glide <paramref name="freqStart"/>→<paramref name="freqEnd"/>,
            /// linear attack, exponential decay (<paramref name="decayPerSecond"/>) and optional vibrato.
            /// </summary>
            public void Tone(float start, float duration, float freqStart, float freqEnd, Wave wave, float amp,
                float attack, float decayPerSecond, float vibratoHz = 0f, float vibratoDepth = 0f)
            {
                int first = (int)(start * _sampleRate);
                int count = (int)(duration * _sampleRate);
                if (count <= 0 || freqStart <= 0f || freqEnd <= 0f) return;

                int attackSamples = Math.Max(1, (int)(attack * _sampleRate));
                int releaseSamples = Math.Min(count, Math.Max(1, _sampleRate / 200)); // 5 ms anti-click tail
                double glide = Math.Pow(freqEnd / (double)freqStart, 1.0 / count);
                double freq = freqStart;
                double phase = 0.0;

                for (int i = 0; i < count; i++)
                {
                    int index = first + i;
                    if (!TryMapIndex(ref index)) break;

                    double t = i / (double)_sampleRate;
                    double f = freq;
                    if (vibratoHz > 0f) f *= 1.0 + vibratoDepth * Math.Sin(TwoPi * vibratoHz * t);
                    phase += f / _sampleRate;
                    phase -= Math.Floor(phase);
                    freq *= glide;

                    double env = Envelope(i, count, attackSamples, releaseSamples, decayPerSecond, t);
                    _buffer[index] += (float)(Oscillator(wave, phase) * amp * env);
                }
            }

            /// <summary>
            /// Filtered white noise. <paramref name="lowpassStart"/>/<paramref name="lowpassEnd"/> are one-pole
            /// coefficients (1 = unfiltered, 0.05 = dull rumble) swept across the duration;
            /// <paramref name="highpass"/> 0..1 removes low end (hiss, hats, sparkles).
            /// </summary>
            public void Noise(float start, float duration, float amp, float attack, float decayPerSecond,
                float lowpassStart, float lowpassEnd, float highpass)
            {
                int first = (int)(start * _sampleRate);
                int count = (int)(duration * _sampleRate);
                if (count <= 0) return;

                int attackSamples = Math.Max(1, (int)(attack * _sampleRate));
                int releaseSamples = Math.Min(count, Math.Max(1, _sampleRate / 200));
                double low = 0.0, slow = 0.0;
                // "slow" tracks the low end; subtracting it is a one-pole high-pass. Higher
                // highpass = faster tracking = more low end removed.
                double hpCoefficient = 0.01 + Clamp01(highpass) * 0.6;

                for (int i = 0; i < count; i++)
                {
                    int index = first + i;
                    if (!TryMapIndex(ref index)) break;

                    double u = i / (double)count;
                    double lp = Clamp01(lowpassStart + (lowpassEnd - lowpassStart) * u);
                    lp = Math.Max(0.01, lp);
                    double white = RandomValue() * 2.0 - 1.0;
                    low += lp * (white - low);

                    double sample = low;
                    if (highpass > 0f)
                    {
                        slow += hpCoefficient * (low - slow);
                        sample = low - slow;
                    }

                    double t = i / (double)_sampleRate;
                    double env = Envelope(i, count, attackSamples, releaseSamples, decayPerSecond, t);
                    _buffer[index] += (float)(sample * amp * env);
                }
            }

            /// <summary>Soft-clips and normalizes to <paramref name="peak"/>; returns the buffer.</summary>
            public float[] Finish(float peak)
            {
                float max = 0f;
                for (int i = 0; i < _buffer.Length; i++)
                {
                    float v = (float)Math.Tanh(_buffer[i]);
                    if (float.IsNaN(v) || float.IsInfinity(v)) v = 0f;
                    _buffer[i] = v;
                    float a = Math.Abs(v);
                    if (a > max) max = a;
                }

                if (max > 0.0001f)
                {
                    float gain = peak / max;
                    for (int i = 0; i < _buffer.Length; i++)
                        _buffer[i] *= gain;
                }
                return _buffer;
            }

            private bool TryMapIndex(ref int index)
            {
                if (index < 0) return false;
                if (index < _buffer.Length) return true;
                if (!_wrap) return false;
                index %= _buffer.Length;
                return true;
            }

            private static double Envelope(int i, int count, int attackSamples, int releaseSamples, float decayPerSecond, double t)
            {
                double env = i < attackSamples ? i / (double)attackSamples : 1.0;
                if (decayPerSecond > 0f) env *= Math.Exp(-decayPerSecond * t);
                int fromEnd = count - i;
                if (fromEnd < releaseSamples) env *= fromEnd / (double)releaseSamples;
                return env;
            }

            private static double Oscillator(Wave wave, double phase)
            {
                switch (wave)
                {
                    case Wave.Square: return phase < 0.5 ? 0.6 : -0.6;
                    case Wave.Saw: return (phase * 2.0 - 1.0) * 0.7;
                    case Wave.Triangle: return phase < 0.5 ? phase * 4.0 - 1.0 : 3.0 - phase * 4.0;
                    default: return Math.Sin(phase * TwoPi);
                }
            }

            private static double Clamp01(double v) => v < 0.0 ? 0.0 : v > 1.0 ? 1.0 : v;
        }

        // ==================================================================== placeholder music

        /// <summary>
        /// Tiny seeded "composer": per-track tempo, scale, chord loop, motif and drum pattern,
        /// rendered into a seamless loop (or a one-shot stinger for victory/defeat).
        /// </summary>
        private static class Music
        {
            private static readonly int[] MinorPentatonic = { 0, 3, 5, 7, 10 };
            private static readonly int[] MajorPentatonic = { 0, 2, 4, 7, 9 };
            private static readonly int[] Miyakobushi = { 0, 1, 5, 7, 8 }; // Japanese "in" scale

            private sealed class Spec
            {
                public float Bpm = 90f;
                public int Bars = 4;
                public int RootMidi = 57;
                public int[] Scale = MinorPentatonic;
                public int[] ChordDegrees = { 0, 3, 2, 4 };
                public Wave PadWave = Wave.Sine;
                public float PadAmp = 0.15f;
                public Wave BassWave = Wave.Sine;
                public float BassAmp = 0.2f;
                public bool BassEighths;
                public Wave LeadWave = Wave.Triangle;
                public float LeadAmp = 0.15f;
                public float LeadDensity = 0.6f;
                public float DrumAmp;
                public int KickMask;   // bit i = 16th step i of the bar
                public int SnareMask;
                public int HatMask;
                public int TomMask;
            }

            public static float[] Compose(string trackId, int sampleRate, uint seed)
            {
                switch (trackId)
                {
                    case AudioCueIds.MusicVictory: return Victory(sampleRate, seed);
                    case AudioCueIds.MusicDefeat: return Defeat(sampleRate, seed);
                }

                var spec = new Spec();
                switch (trackId)
                {
                    case AudioCueIds.MusicMenu:
                        spec.Bpm = 76f; spec.RootMidi = 57; spec.Scale = MinorPentatonic;
                        spec.ChordDegrees = new[] { 0, 3, 2, 4 };
                        spec.PadAmp = 0.16f; spec.LeadAmp = 0.14f; spec.LeadDensity = 0.55f;
                        spec.BassAmp = 0.16f;
                        break;
                    case AudioCueIds.MusicVillage:
                        spec.Bpm = 92f; spec.RootMidi = 62; spec.Scale = MajorPentatonic;
                        spec.ChordDegrees = new[] { 0, 3, 4, 2 };
                        spec.PadWave = Wave.Triangle; spec.PadAmp = 0.1f;
                        spec.BassWave = Wave.Triangle; spec.BassAmp = 0.2f;
                        spec.LeadAmp = 0.16f; spec.LeadDensity = 0.7f;
                        spec.DrumAmp = 0.35f; spec.KickMask = Steps(0, 8); spec.HatMask = Steps(2, 6, 10, 14);
                        break;
                    case AudioCueIds.MusicBattle:
                        spec.Bpm = 132f; spec.RootMidi = 52; spec.Scale = Miyakobushi;
                        spec.ChordDegrees = new[] { 0, 0, 3, 2 };
                        spec.PadWave = Wave.Saw; spec.PadAmp = 0.06f;
                        spec.BassWave = Wave.Saw; spec.BassAmp = 0.2f; spec.BassEighths = true;
                        spec.LeadWave = Wave.Square; spec.LeadAmp = 0.08f; spec.LeadDensity = 0.8f;
                        spec.DrumAmp = 0.6f; spec.KickMask = Steps(0, 4, 8, 12); spec.SnareMask = Steps(4, 12);
                        spec.HatMask = Steps(2, 6, 10, 14);
                        break;
                    case AudioCueIds.MusicBoss:
                        spec.Bpm = 150f; spec.RootMidi = 50; spec.Scale = Miyakobushi;
                        spec.ChordDegrees = new[] { 0, 1, 0, 3 };
                        spec.PadWave = Wave.Saw; spec.PadAmp = 0.08f;
                        spec.BassWave = Wave.Saw; spec.BassAmp = 0.24f; spec.BassEighths = true;
                        spec.LeadWave = Wave.Square; spec.LeadAmp = 0.09f; spec.LeadDensity = 0.9f;
                        spec.DrumAmp = 0.7f; spec.KickMask = Steps(0, 3, 6, 8, 11); spec.SnareMask = Steps(4, 12);
                        spec.HatMask = Steps(0, 2, 4, 6, 8, 10, 12, 14); spec.TomMask = Steps(13, 14, 15);
                        break;
                    default:
                        // Unknown track: a slow ambient drone so something still plays.
                        spec.Bpm = 70f; spec.Scale = MinorPentatonic; spec.LeadDensity = 0.3f;
                        break;
                }

                return Render(spec, sampleRate, seed);
            }

            private static float[] Render(Spec spec, int sampleRate, uint seed)
            {
                float beat = 60f / spec.Bpm;
                float bar = beat * 4f;
                float step16 = beat / 4f;
                var s = new Synth(sampleRate, bar * spec.Bars, seed, wrap: true);

                // A one-bar motif (8 eighth-note slots) reused every bar, shifted by the chord —
                // repetition is what makes a loop sound like music instead of noise.
                var motif = new int[8];
                var motifOn = new bool[8];
                int degree = 2;
                for (int i = 0; i < 8; i++)
                {
                    degree += (int)s.RandomRange(-2f, 2.99f);
                    degree = Math.Max(0, Math.Min(7, degree));
                    motif[i] = degree;
                    motifOn[i] = i == 0 || s.RandomValue() < spec.LeadDensity;
                }

                for (int b = 0; b < spec.Bars; b++)
                {
                    float barStart = b * bar;
                    int chord = spec.ChordDegrees[b % spec.ChordDegrees.Length];

                    // Pad: stacked scale "thirds" of the chord degree, long attack.
                    for (int voice = 0; voice < 3; voice++)
                    {
                        float f = MidiToHz(spec.RootMidi + Semitones(spec.Scale, chord + voice * 2));
                        s.Tone(barStart, bar * 1.1f, f, f, spec.PadWave, spec.PadAmp, Math.Min(0.6f, bar * 0.25f), 0.4f);
                    }

                    // Bass on the chord root.
                    float bassHz = MidiToHz(spec.RootMidi - 12 + Semitones(spec.Scale, chord));
                    int bassNotes = spec.BassEighths ? 8 : 4;
                    float bassStep = bar / bassNotes;
                    for (int n = 0; n < bassNotes; n++)
                        s.Tone(barStart + n * bassStep, bassStep * 0.9f, bassHz, bassHz, spec.BassWave, spec.BassAmp, 0.005f, 5f);

                    // Lead motif, an octave up, transposed with the chord.
                    for (int i = 0; i < 8; i++)
                    {
                        if (!motifOn[i]) continue;
                        float f = MidiToHz(spec.RootMidi + 12 + Semitones(spec.Scale, motif[i] + chord));
                        s.Tone(barStart + i * beat * 0.5f, beat * 0.7f, f, f, spec.LeadWave, spec.LeadAmp, 0.01f, 4f);
                    }

                    // Drums.
                    if (spec.DrumAmp <= 0f) continue;
                    for (int step = 0; step < 16; step++)
                    {
                        float t = barStart + step * step16;
                        if ((spec.KickMask & (1 << step)) != 0)
                            s.Tone(t, 0.2f, 120f, 45f, Wave.Sine, spec.DrumAmp, 0.002f, 14f);
                        if ((spec.SnareMask & (1 << step)) != 0)
                        {
                            s.Noise(t, 0.15f, spec.DrumAmp * 0.5f, 0.001f, 22f, 0.7f, 0.5f, 0.15f);
                            s.Tone(t, 0.08f, 220f, 180f, Wave.Triangle, spec.DrumAmp * 0.25f, 0.001f, 30f);
                        }
                        if ((spec.HatMask & (1 << step)) != 0)
                            s.Noise(t, 0.04f, spec.DrumAmp * 0.2f, 0.001f, 80f, 1f, 1f, 0.6f);
                        if ((spec.TomMask & (1 << step)) != 0)
                            s.Tone(t, 0.3f, 110f - step * 2f, 70f, Wave.Sine, spec.DrumAmp * 0.7f, 0.002f, 9f);
                    }
                }

                return s.Finish(0.85f);
            }

            private static float[] Victory(int sampleRate, uint seed)
            {
                var s = new Synth(sampleRate, 3.2f, seed);
                float[] fanfare = { 392f, 523.3f, 659.3f, 784f };
                for (int i = 0; i < fanfare.Length; i++)
                {
                    s.Tone(i * 0.15f, 0.3f, fanfare[i], fanfare[i], Wave.Square, 0.12f, 0.005f, 4f);
                    s.Tone(i * 0.15f, 0.3f, fanfare[i], fanfare[i], Wave.Triangle, 0.2f, 0.005f, 4f);
                }
                foreach (float f in new[] { 523.3f, 659.3f, 784f, 1046.5f })
                    s.Tone(0.65f, 2.4f, f, f, Wave.Triangle, 0.15f, 0.02f, 1.2f, 5f, 0.004f);
                s.Tone(0.65f, 2.4f, 130.8f, 130.8f, Wave.Saw, 0.15f, 0.02f, 1.2f);
                s.Tone(0.65f, 0.25f, 120f, 45f, Wave.Sine, 0.6f, 0.002f, 12f);
                s.Noise(0.65f, 1.5f, 0.25f, 0.002f, 3f, 1f, 1f, 0.6f);
                return s.Finish(0.85f);
            }

            private static float[] Defeat(int sampleRate, uint seed)
            {
                var s = new Synth(sampleRate, 3.6f, seed);
                s.Tone(0f, 3.5f, 110f, 104f, Wave.Saw, 0.12f, 0.3f, 0.8f);
                float[] line = { 329.6f, 293.7f, 261.6f, 246.9f };
                for (int i = 0; i < line.Length; i++)
                    s.Tone(i * 0.45f, 0.6f, line[i], line[i], Wave.Triangle, 0.3f, 0.01f, 2.5f, 5f, 0.006f);
                foreach (float f in new[] { 220f, 261.6f, 329.6f })
                    s.Tone(1.8f, 1.7f, f, f, Wave.Sine, 0.18f, 0.05f, 1.5f);
                return s.Finish(0.8f);
            }

            private static int Semitones(int[] scale, int degree)
            {
                int n = scale.Length;
                int octave = degree >= 0 ? degree / n : (degree - n + 1) / n;
                int index = degree - octave * n;
                return scale[index] + 12 * octave;
            }

            private static int Steps(params int[] steps)
            {
                int mask = 0;
                foreach (int step in steps) mask |= 1 << step;
                return mask;
            }
        }
    }
}
