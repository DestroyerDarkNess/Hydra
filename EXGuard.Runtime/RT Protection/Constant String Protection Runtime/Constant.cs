using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;

using static Lzma;

namespace EXGuard.Runtime.RTProtection
{
    internal static unsafe class Constant
    {
        private static byte[] Data = null;

        public static void Initialize()
        {
            var l = (uint)Mutation.IntKey0;

            if (l != 0)
            {
                uint[] q = new uint[l];
                RuntimeHelpers.InitializeArray(q, Mutation.LocationIndex<RuntimeFieldHandle>());

                var n = (uint)Mutation.IntKey1;

                var k = new uint[0x10];
                for (int i = 0; i < 0x10; i++)
                {
                    n ^= n >> 12;
                    n ^= n << 25;
                    n ^= n >> 27;

                    k[i] = (uint)n;
                }

                int s = 0, d = 0;
                var w = new uint[0x10];
                var o = new byte[l * 4];
                while (s < l)
                {
                    for (int j = 0; j < 0x10; j++)
                        w[j] = q[s + j];

                    Mutation.Crypt(w, k);

                    for (int j = 0; j < 0x10; j++)
                    {
                        uint e = w[j];
                        o[d++] = (byte)e;
                        o[d++] = (byte)(e >> 8);
                        o[d++] = (byte)(e >> 16);
                        o[d++] = (byte)(e >> 24);
                        k[j] ^= e;
                    }

                    s += 0x10;
                }

                Data = Decompress(o);
            }
        }

        public static unsafe string Get(int id, int index, RuntimeMethodHandle handle)
        {
            MethodBase method = MethodBase.GetMethodFromHandle(handle);

            byte[] il = method.GetMethodBody().GetILAsByteArray();
            int key = il[index] | il[index + 1] << 8 | il[index + 2] << 16 | il[index + 3] << 24;

            id ^= key;
            id = Mutation.Placeholder(id);
            id = (id & 0x3fffffff) << 2;

            int l = Data[id] | Data[id + 1] << 8 | Data[id + 2] << 16 | Data[id + 3] << 24;
            return string.Intern(Encoding.UTF8.GetString(Data, id + 4, l));
        }
    }
}