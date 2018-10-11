using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DayCastClient
{
    public static class DayCastServer
    {
        private static Process ServerProcess;

        public static bool TryLaunchServer(string HostIPAddress, string HostPort)
        {
            try
            {
                ServerProcess = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "dotnet",
                    Arguments = $"netcoreapp2.1\\DayCastServer.dll {HostIPAddress} {HostPort}"
                };
                ServerProcess.StartInfo = startInfo;
                ServerProcess.Start();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryStopServer()
        {
            try
            {
                ServerProcess.Kill();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
