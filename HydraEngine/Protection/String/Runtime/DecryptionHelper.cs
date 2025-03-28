﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.String.Runtime
{
    internal static class DecryptionHelper
    {
        /// <summary>
        /// Method for decrypt string with Base64.
        /// </summary>
        /// <param name="dataEnc">Input encode string</param>
        /// <returns>Plain string</returns>
        public static string Decrypt_Base64(string dataEnc)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(dataEnc));
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
