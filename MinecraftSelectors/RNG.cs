using System;
using System.Collections.Generic;
using System.Linq;

namespace MinecraftSelectors {
    public static class RNG {
        private static readonly Random TheRandom = new Random();

        public static float NextFloat(float range) {
            return (float) TheRandom.NextDouble() * range;
        }

        public static double NextDouble(double range) {
            return TheRandom.NextDouble() * range;
        }

        public static bool Percent(double percent) {
            return NextDouble(100) <= Math.Max(0, Math.Min(100, percent));
        }

        public static int NextInt(int range) {
            return TheRandom.Next(range) + 1;
        }

        public static bool NextBoolean() {
            return TheRandom.NextDouble() < 0.5;
        }

        public static T OfList<T>(List<T> list) {
            return list[TheRandom.Next(list.Count)];
        }

        public static T Random<T>(this IEnumerable<T> list) {
            return OfList(list.ToList());
        }

        public static int Range(int start, int end) {
            return NextInt(end - start) + start;
        }

        public static float Range(float start, float end) {
            return NextFloat(end - start) + start;
        }

        public static double Range(double start, double end) {
            return NextDouble(end - start) + start;
        }

        public static TEnum Random<TEnum>(this TEnum of) where TEnum : Enum {
            return OfList(Enum.GetValues(of.GetType()).ToArray<TEnum>().ToList());
        }

        public static List<T> Randomize<T>(this IEnumerable<T> enumerable) {
            var unused = enumerable.ToList();
            var list = new List<T>();
            while (unused.Count > 0) {
                var e = unused.Random();
                list.Add(e);
                unused.Remove(e);
            }

            return list;
        }
    }
}