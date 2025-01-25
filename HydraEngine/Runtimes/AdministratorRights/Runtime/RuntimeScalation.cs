using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace HydraEngine.Runtimes.AdministratorRights
{
    internal static class RuntimeScalation
    {

        private static void Initialize()
        {
            string AppDir = Path.GetFullPath(System.Windows.Forms.Application.ExecutablePath);
            bool isElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
           
            if (isElevated == false)
            {

                ProcessStartInfo procStartInfo = new ProcessStartInfo();
                Process procExecuting = new Process();

                procStartInfo.UseShellExecute = true;
                procStartInfo.FileName = AppDir;
                procStartInfo.WindowStyle = ProcessWindowStyle.Normal;
                procStartInfo.Verb = "runas";
                procStartInfo.Arguments = Environment.CommandLine;

                procExecuting = Process.Start(procStartInfo);

                Process.GetCurrentProcess().Kill();
            }

        }

    }
}
