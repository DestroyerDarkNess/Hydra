using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.Anti.Runtime
{
    internal static class AntiHttpRuntime
    {
        internal static void Initialize()
        {
            WebRequest.DefaultWebProxy = new WebProxy();
        }
    }
}
