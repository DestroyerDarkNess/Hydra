namespace VM.Runtime
{
    internal static class XXHash
    {
        internal static uint CalculateXXHash32(string input)
        {
            const uint prime1 = 2654435761U;
            const uint prime2 = 2246822519U;
            const uint prime3 = 3266489917U;
            const uint prime4 = 668265263U;
            const uint prime5 = 374761393U;

            int len = input.Length;
            int index = 0;
            uint seed = 0; // You can change the seed value if needed

            uint h32;

            if (len >= 16)
            {
                int limit = len - 16;
                uint v1 = seed + prime1;
                uint v2 = seed + prime2;
                uint v3 = seed + prime3;
                uint v4 = seed + prime4;

                do
                {
                    uint block1 = GetUInt32(input, index) * prime2;
                    index += 4;
                    uint block2 = GetUInt32(input, index) * prime2;
                    index += 4;

                    v1 = Rol(v1 + block1, 13) * prime1;
                    v2 = Rol(v2 + block2, 13) * prime1;
                    v1 = Rol(v1, 13);
                    v2 = Rol(v2, 13);

                    uint block3 = GetUInt32(input, index) * prime2;
                    index += 4;
                    uint block4 = GetUInt32(input, index) * prime2;
                    index += 4;

                    v3 = Rol(v3 + block3, 13) * prime1;
                    v4 = Rol(v4 + block4, 13) * prime1;
                    v3 = Rol(v3, 13);
                    v4 = Rol(v4, 13);
                } while (index <= limit);

                h32 = Rol(v1, 1) + Rol(v2, 7) + Rol(v3, 12) + Rol(v4, 18);
            }
            else
            {
                h32 = seed + prime5;
            }

            h32 += (uint)len;

            while (index <= len - 4)
            {
                h32 += GetUInt32(input, index) * prime3;
                h32 = Rol(h32, 17) * prime4;
                index += 4;
            }

            while (index < len)
            {
                h32 += input[index] * prime5;
                h32 = Rol(h32, 11) * prime1;
                index++;
            }

            h32 ^= h32 >> 15;
            h32 *= prime2;
            h32 ^= h32 >> 13;
            h32 *= prime3;
            h32 ^= h32 >> 16;

            return h32;
        }

        private static uint GetUInt32(string input, int index) => (uint)input[index] | ((uint)input[index + 1] << 8) | ((uint)input[index + 2] << 16) | ((uint)input[index + 3] << 24);

        private static uint Rol(uint value, int count) => (value << count) | (value >> (32 - count));
    }
}