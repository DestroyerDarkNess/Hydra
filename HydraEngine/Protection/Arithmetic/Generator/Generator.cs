using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic_Obfuscation.Arithmetic.Generator
{
    public class Generator
    {
        private Random random;
        public Generator()
        {
            random = new Random(Guid.NewGuid().GetHashCode());
        }
        public int Next()
        {
            return random.Next(int.MaxValue);
        }
        public int Next(int value)
        {
            return random.Next(value);
        }
        public int Next(int min, int max)
        {
            return random.Next(min, max);
        }
    }
}
