using System;

namespace AetherArk.Core
{
    public static class SeededRandom
    {
        public static uint Seed(int seed, uint salt)
        {
            var value = unchecked((uint)seed) ^ salt;
            if (value == 0) value = 0x6D2B79F5u;
            return NextRaw(ref value);
        }

        public static uint NextRaw(ref uint state)
        {
            if (state == 0) state = 0x6D2B79F5u;
            var value = state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            state = value;
            return value;
        }

        public static float Value(ref uint state)
        {
            return (NextRaw(ref state) & 0x00FFFFFFu) / 16777216f;
        }

        public static int Range(ref uint state, int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextRaw(ref state) % (uint)(maxExclusive - minInclusive));
        }

        public static bool Chance(ref uint state, float probability)
        {
            return Value(ref state) < Math.Max(0f, Math.Min(1f, probability));
        }
    }
}
