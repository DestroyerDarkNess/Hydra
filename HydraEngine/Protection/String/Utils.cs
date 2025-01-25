using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Protection.String
{
    internal class Utils
    {
        /// <summary>
        /// Method for encrypt string with Base64. 
        /// </summary>
        /// <param name="dataPlain">Input plain string</param>
        /// <returns>Encode string</returns>
        public static string Encrypt_Base64(string dataPlain)
        {
            try
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(dataPlain));
            }

            catch (Exception)
            {
                return null;
            }
        }
    }
}
