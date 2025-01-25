using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using dnlib.W32Resources;
using VM.Runtime.Handler.Impl;

public static class Runtime
{

    public static string Recover(string processedStr)
    {
        /*var Method = new StackTrace().GetFrame(1)!.GetMethod();
        int MDToken = Method.MetadataToken;
        int MaxStack = Method.GetMethodBody().MaxStackSize;*/

        byte[] data = processedStr.Split('|').Select(s => byte.Parse(s)).ToArray();

        using (MemoryStream input = new MemoryStream(data))
        using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
        using (MemoryStream output = new MemoryStream())
        {
            dstream.CopyTo(output);

            data = output.ToArray();
            /*for (var i = 0; i < data.Length; i++)
            {
                //data[i] = (byte)((byte)((BitConverter.GetBytes(MDToken ^ MaxStack)[i % sizeof(int)] ^ (data[i]))));
                data[i] = (byte)((byte)((BitConverter.GetBytes(test)[i % sizeof(int)] ^ (data[i]))));
            }*/

            return Encoding.UTF8.GetString(data);
        }
    }
}
