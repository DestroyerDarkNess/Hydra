using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Core
{
    public class Randomizer
    {
        public static readonly RandomNumberGenerator csp = RandomNumberGenerator.Create();
      
        public static int Next(int maxValue, int minValue = 0)
        {
            if (minValue >= maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
            long diff = (long)maxValue - minValue;
            long upperBound = uint.MaxValue / diff * diff;
            uint ui;
            do { ui = RandomUInt(); } while (ui >= upperBound);
            return (int)(minValue + (ui % diff));
        }

        private static Random random = new Random();
        private static string BaseChars { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public static string BaseChars2 = "日本書紀العالمحالعجلة林氏家族การดำน้ำดูปะการังसंस्कृतम्संस्कृतावाक्" +
             "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
             "0123456789ABCDEFGHIJKLMNÑOPQRSTUVWXYZ" +
             "αβγδεζηθικλµνξοπρστυϕχψω" +
             "れづれなるまゝに日暮らし硯にむかひて心にうりゆくよな事を、こはかとなく書きつくればあやうこそものぐるほけれ。"; 

        public static string GenerateRandomString(string chars, int length)
        {
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateRandomSpaces(int minSpaces, int maxSpaces)
        {
            if (minSpaces < 0 || maxSpaces < minSpaces)
            {
                throw new ArgumentException("Los valores de minSpaces y maxSpaces deben ser no negativos y minSpaces no puede ser mayor que maxSpaces.");
            }

            int numberOfSpaces = random.Next(minSpaces, maxSpaces + 1);

            return new string(' ', numberOfSpaces);
        }

        public static string GenerateRandomString()
        {
            return new string(Enumerable.Repeat(BaseChars, random.Next(25, 100)).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateRandomString2()
        {
            return new string(Enumerable.Repeat(BaseChars2, random.Next(25, 100)).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string GenerateRandomString(int size)
        {
            StringBuilder stringy = new StringBuilder();
            for (int i = 0; i < size; i++)
            {
                var renamer = "_" + Next(100000, 10000);
                stringy.Append(renamer);
            }
            return stringy.ToString();
        }

        private static uint RandomUInt()
        {
            return BitConverter.ToUInt32(RandomBytes(sizeof(uint)), 0);
        }

        private static byte[] RandomBytes(int bytesNumber)
        {
            byte[] buffer = new byte[bytesNumber];
            csp.GetNonZeroBytes(buffer);
            return buffer;
        }
    }
}
