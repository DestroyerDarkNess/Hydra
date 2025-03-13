using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;

using dnlib.DotNet.Writer;

namespace EXGuard.Core.Services
{
    /// <summary>
    ///     A seeded SHA256 PRNG.
    public class RandomGenerator
    {
        /// <summary>
        ///     The prime numbers used for generation
        /// </summary>
        private static readonly byte[] primes = { 7, 11, 23, 37, 43, 59, 71 };

        private static readonly RNGCryptoServiceProvider _RNG = new RNGCryptoServiceProvider();
        private readonly SHA256Managed sha256 = new SHA256Managed();
        int mixIndex;
        byte[] state; //32 bytes
        int stateFilled;
        int seedLen;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RandomGenerator" /> class. (32 in length random seed)
        /// </summary>
        internal RandomGenerator()
        {
            byte[] seed = new byte[32];
            _RNG.GetBytes(seed);

            state = _SHA256((byte[])seed.Clone());
            seedLen = seed.Length;
            stateFilled = 32;
            mixIndex = 0;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RandomGenerator" /> class.
        /// </summary>
        /// <param name="length">The number of random seed.</param>
        internal RandomGenerator(int length)
        {
            byte[] seed = new byte[(length == 0 ? 32 : length)];
            _RNG.GetBytes(seed);

            state = _SHA256((byte[])seed.Clone());
            seedLen = seed.Length;
            stateFilled = 32;
            mixIndex = 0;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RandomGenerator" /> class.
        /// </summary>
        /// <param name="seed">The seed.</param>
        internal RandomGenerator(string seed)
        {
            byte[] ret = _SHA256((byte[])(!string.IsNullOrEmpty(seed) ? Encoding.UTF8.GetBytes(seed) : Guid.NewGuid().ToByteArray()).Clone());
            for (int i = 0; i < 32; i++)
            {
                ret[i] *= primes[i % primes.Length];
                ret = _SHA256(ret);
            }

            state = ret;
            seedLen = ret.Length;
            stateFilled = 32;
            mixIndex = 0;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RandomGenerator" /> class.
        /// </summary>
        /// <param name="seed">The seed.</param>
        internal RandomGenerator(byte[] seed)
        {
            state = (byte[])seed.Clone();
            seedLen = seed.Length;
            stateFilled = 32;
            mixIndex = 0;
        }

        /// <summary>
		///     Compute the SHA256 hash of the input buffer.
		/// </summary>
		/// <param name="buffer">The input buffer.</param>
		/// <returns>The SHA256 hash of the input buffer.</returns>
		private static byte[] _SHA256(byte[] buffer) {
            var sha = new SHA256Managed();
            return sha.ComputeHash(buffer);
        }

        /// <summary>
        ///     Refills the state buffer.
        /// </summary>
        void NextState()
        {
            for (int i = 0; i < 32; i++)
                state[i] ^= primes[mixIndex = (mixIndex + 1) % primes.Length];
            state = sha256.ComputeHash(state);
            stateFilled = 32;
        }

        /// <summary>
        ///     Fills the specified buffer with random bytes.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset of buffer to fill in.</param>
        /// <param name="length">The number of random bytes.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="buffer" /> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        ///     <paramref name="offset" /> or <paramref name="length" /> is less than 0.
        /// </exception>
        /// <exception cref="System.ArgumentException">Invalid <paramref name="offset" /> or <paramref name="length" />.</exception>
        public void NextBytes(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            if (buffer.Length - offset < length)
                throw new ArgumentException("Invalid offset or length.");

            while (length > 0)
            {
                if (length >= stateFilled)
                {
                    Buffer.BlockCopy(state, 32 - stateFilled, buffer, offset, stateFilled);
                    offset += stateFilled;
                    length -= stateFilled;
                    stateFilled = 0;
                }
                else
                {
                    Buffer.BlockCopy(state, 32 - stateFilled, buffer, offset, length);
                    stateFilled -= length;
                    length = 0;
                }
                if (stateFilled == 0)
                    NextState();
            }
        }

        /// <summary>
        ///     Returns a random byte.
        /// </summary>
        /// <returns>Requested random byte.</returns>
        public byte NextByte()
        {
            byte ret = state[32 - stateFilled];
            stateFilled--;
            if (stateFilled == 0)
                NextState();
            return ret;
        }

        /// <summary>
        ///     Gets a random string with the specified length.
        /// </summary>
        /// <param name="length">The number of random string.</param>
        /// <returns>Requested random string.</returns>
        public string NextString(int length)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                char ch;
                for (int i = 0; i < length; i++)
                {
                    ch = System.Convert.ToChar(System.Convert.ToInt32(System.Math.Floor(32 + (decimal)NextInt32('z') - 32)));
                    builder.Append(ch);
                }
                return builder.ToString();
            }
            catch { /* throw null */ };

            return string.Empty;
        }

        /// <summary>
        ///     Gets a random hex string with the specified length.
        /// </summary>
        /// <param name="length">The number of random hex string.</param>
        /// <returns>Requested random hex string.</returns>
        public string NextHexString(int length, bool large = false)
        {
            if (length.ToString().Contains("5"))
            {
                throw new Exception("5 is an unacceptable number!");
            }

            try
            {
                var chars = @"qwertyuıopğüasdfghjklşizxcvbnmöçQWERTYUIOPĞÜASDFGHJKLŞİZXCVBNMÖÇ0123456789/*-.:,;!'^+%&/()=?_~|\}][{½$#£>";
                var rnd = new string(Enumerable.Repeat(chars, (length / 2)).Select(s => s[NextInt32(s.Length)]).ToArray());
               
                if (large == false)
                    return BitConverter.ToString(Encoding.Default.GetBytes(rnd)).Replace("-", string.Empty).ToLower();
                else if (large == true)
                    return BitConverter.ToString(Encoding.Default.GetBytes(rnd)).Replace("-", string.Empty);
            }
            catch { /* throw null */ };

            return string.Empty;
        }

        /// <summary>
        ///     Returns a random hex string.
        /// </summary>
        /// <returns>Requested random hex string.</returns>
        public string NextHexString(bool large = false)
        {
            return NextHexString(8, large);
        }

        /// <summary>
        ///     Returns a random string.
        /// </summary>
        /// <returns>Requested random string.</returns>
        public string NextString()
        {
            return NextString(seedLen);
        }

        /// <summary>
        ///     Gets a buffer of random bytes with the specified length.
        /// </summary>
        /// <param name="length">The number of random bytes.</param>
        /// <returns>A buffer of random bytes.</returns>
        public byte[] NextBytes(int length)
        {
            var ret = new byte[length];
            NextBytes(ret, 0, length);
            return ret;
        }

        /// <summary>
        ///     Gets a buffer of random bytes.
        /// </summary>
        /// <returns>A buffer of random bytes.</returns>
        public byte[] NextBytes()
        {
            var ret = new byte[seedLen];
            NextBytes(ret, 0, seedLen);
            return ret;
        }

        /// <summary>
        ///     Returns a random signed integer.
        /// </summary>
        /// <returns>Requested random number.</returns>
        public int NextInt32()
        {
            return BitConverter.ToInt32(NextBytes(4), 0);
        }

        /// <summary>
        ///     Returns a nonnegative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="max">The exclusive upper bound.</param>
        /// <returns>Requested random number.</returns>
        public int NextInt32(int max)
        {
            return (int)(NextUInt32() % max);
        }

        /// <summary>
        ///     Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="min">The inclusive lower bound.</param>
        /// <param name="max">The exclusive upper bound.</param>
        /// <returns>Requested random number.</returns>
        public int NextInt32(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt32() % (max - min));
        }

        /// <summary>
        ///     Returns a random unsigned integer.
        /// </summary>
        /// <returns>Requested random number.</returns>
        public uint NextUInt32()
        {
            return BitConverter.ToUInt32(NextBytes(4), 0);
        }

        /// <summary>
        ///     Returns a nonnegative random integer that is less than the specified maximum.
        /// </summary>
        /// <param name="max">The exclusive upper bound.</param>
        /// <returns>Requested random number.</returns>
        public uint NextUInt32(uint max) => NextUInt32() % max;

        /// <summary>
        ///     Returns a random double floating pointer number from 0 (inclusive) to 1 (exclusive).
        /// </summary>
        /// <returns>Requested random number.</returns>
        public double NextDouble()
        {
            return NextUInt32() / ((double)uint.MaxValue + 1);
        }
        
        /// <summary>
        ///     Returns a random double that is within a specified range..
        /// </summary>
        /// <param name="min">The inclusive lower bound.</param>
        /// <param name="max">The exclusive upper bound.</param>
        /// <returns>Requested random double.</returns>
        public double NextDouble(double min, double max) => NextDouble() * (max - min) + min;

        /// <summary>
        ///     Returns a random boolean value.
        /// </summary>
        /// <returns>Requested random boolean value.</returns>
        public bool NextBoolean()
        {
            byte s = state[32 - stateFilled];
            stateFilled--;
            if (stateFilled == 0)
                NextState();
            return s % 2 == 0;
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 1; i--)
            {
                int k = NextInt32(i + 1);
                T tmp = list[k];
                list[k] = list[i];
                list[i] = tmp;
            }
        }

        /// <summary>
        ///     Shuffles the element in the specified metadata table.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="table">The metadata table to shuffle.</param>
        public void Shuffle<T>(MDTable<T> table) where T : struct
        {
            if (table.IsEmpty) return;

            for (uint i = (uint)(table.Rows); i > 2; i--)
            {
                uint k = NextUInt32(i - 1) + 1;
                Debug.Assert(k >= 1, $"{nameof(k)} >= 1");
                Debug.Assert(k < i, $"{nameof(k)} < {nameof(i)}");
                Debug.Assert(k <= table.Rows, $"{nameof(k)} <= {nameof(table)}.Rows");

                var tmp = table[k];
                table[k] = table[i];
                table[i] = tmp;
            }
        }
    }
}