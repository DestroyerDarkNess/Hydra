using System;

namespace VM.Core.Helper
{
    public class Generator
    {
        public static Random Random = new Random();
        public static string RandomName() => $"[{Guid.NewGuid().ToString().ToUpper()}]".Replace("[", "{").Replace("]", "}");
        public static int NextInt(int min, int max) => new Random(Guid.NewGuid().GetHashCode()).Next(min, max + 1);
    }
}