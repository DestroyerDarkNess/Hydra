using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;

using dnlib.DotNet;

using EXGuard.Core.Helpers;

using EXGuard.Core.Services.LZMA;
using EXGuard.Core.Helpers.System;
using EXGuard.Core.Services.LZMA.Base;

namespace EXGuard.Core.Services {
	public class CompressionService {
		public byte[] LZMA_Compress(byte[] data, Action<double> progressFunc = null) {
            CoderPropID[] propIDs = {
                CoderPropID.DictionarySize,
                CoderPropID.PosStateBits,
                CoderPropID.LitContextBits,
                CoderPropID.LitPosBits,
                CoderPropID.Algorithm,
                CoderPropID.NumFastBytes,
                CoderPropID.MatchFinder,
                CoderPropID.EndMarker
            };

            object[] properties = {
                1 << 23,
                2,
                3,
                0,
                2,
                128,
                "bt4",
                false
            };

            var x = new MemoryStream();
            var encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(x);

            var length = BitConverter.GetBytes(data.Length);
            if (!BitConverter.IsLittleEndian)
            {
                var i = length.GetLowerBound(0);
                var j = length.GetLowerBound(0) + sizeof(int) - 1;
                var objArray = (Array)length as object[];
                if (objArray != null)
                {
                    while (i < j)
                    {
                        var temp = objArray[i];
                        objArray[i] = objArray[j];
                        objArray[j] = temp;
                        i++;
                        j--;
                    }
                }
                else
                {
                    while (i < j)
                    {
                        var temp = length.GetValue(i);
                        length.SetValue(length.GetValue(j), i);
                        length.SetValue(temp, j);
                        i++;
                        j--;
                    }
                }
            }

            // Store 4 byte length value (little-endian)
            x.Write(length, 0, sizeof(int));

            ICodeProgress progress = null;
			if (progressFunc != null)
				progress = new CompressionLogger(progressFunc, data.Length);
			encoder.Code(new MemoryStream(data), x, -1, -1, progress);

			return x.ToArray();
		}

        public byte[] GZIP_Compress(byte[] inputData)
        {
            using (var compressIntoMs = new MemoryStream())
            {
                using (var gzs = new GZipStream(compressIntoMs, CompressionMode.Compress))
                {
                    gzs.Write(inputData, 0, inputData.Length);
                }
                return compressIntoMs.ToArray();
            }
        }

        class CompressionLogger : ICodeProgress {
			readonly Action<double> progressFunc;
			readonly int size;

			public CompressionLogger(Action<double> progressFunc, int size) {
				this.progressFunc = progressFunc;
				this.size = size;
			}

			public void SetProgress(long inSize, long outSize) {
				double precentage = (double)inSize / size;
				progressFunc(precentage);
			}
		}
	}
}