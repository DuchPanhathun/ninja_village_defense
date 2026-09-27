namespace NinjaVillage.Systems.Economy
{
    /// <summary>
    /// Tiny deterministic PRNG (xorshift32) for anything that must roll the same result on every
    /// device and every run of the game for the same seed — the Market's daily stock and the
    /// village's decoration layout. System.Random's sequence is an implementation detail of the
    /// runtime and string.GetHashCode is randomized per process, so neither is safe for that.
    /// </summary>
    public struct SeededRandom
    {
        private uint _state;

        public SeededRandom(int seed)
        {
            // Scramble the seed so nearby seeds (day 100, day 101) give unrelated sequences,
            // and never start at 0 (xorshift's fixed point).
            uint s = unchecked((uint)seed * 2654435761u) ^ 0x9E3779B9u;
            _state = s == 0 ? 0x6D2B79F5u : s;
            NextUInt(); // discard the first, weakly-mixed value
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>[0, 1).</summary>
        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>[minInclusive, maxExclusive). Returns minInclusive when the range is empty.</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            uint span = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % span);
        }

        /// <summary>[min, max].</summary>
        public float Range(float min, float max) => min + (max - min) * NextFloat();
    }

    /// <summary>Process-independent string hash (FNV-1a) for building seeds from ids.</summary>
    public static class StableHash
    {
        public static int Fnv1a(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (!string.IsNullOrEmpty(text))
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        hash ^= text[i];
                        hash *= 16777619u;
                    }
                }
                return (int)hash;
            }
        }

        public static int Combine(int a, int b)
        {
            unchecked { return (a * 397) ^ b; }
        }
    }
}
