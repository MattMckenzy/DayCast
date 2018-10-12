using DayCastClient.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DayCastClient
{
    public static class DayCastServer
    {
        public static bool TryLaunchServer(string HostIPAddress, string HostPort)
        {
            try
            {
                Process serverProcess = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "dotnet",
                    Arguments = $"netcoreapp2.1\\DayCastServer.dll {HostIPAddress} {HostPort}"
                };
                serverProcess.StartInfo = startInfo;
                serverProcess.Start();

                Settings.Default.ServerProcessId = serverProcess.Id;

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
                Process serverProcess = Process.GetProcesses().FirstOrDefault(p => p.Id == Settings.Default.ServerProcessId);

                if (serverProcess == null)
                    throw new Exception("Server not found!");

                Settings.Default.ServerProcessId = -1;
                PollingServer = false;

                serverProcess.Kill();

                return true;
            }
            catch
            {
                Settings.Default.ServerProcessId = -1;
                return false;
            }
        }


        #region DayCastServer HTTP Communicatoin

        public static event EventHandler<ServerQueueReceptionEventArgs> ServerQueueReception;

        public class ServerQueueReceptionEventArgs : EventArgs
        {
            public IEnumerable<string> ReceivedQueueItemPaths;
        }

        private static readonly HttpClient client = new HttpClient();

        public static bool PollingServer = false;

        public async static void PollServer(string HostAddress)
        {
            await Task.Factory.StartNew(async () =>
            {
                PollingServer = true;

                while (PollingServer)
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync($"{HostAddress}/dequeue");
                        IEnumerable<string> receivedQueueItemPaths = JsonConvert.DeserializeObject<IEnumerable<string>>(await response.Content.ReadAsStringAsync());

                        if (receivedQueueItemPaths.Count() > 0)
                            ServerQueueReception?.Invoke(null, new ServerQueueReceptionEventArgs() { ReceivedQueueItemPaths = receivedQueueItemPaths });
                    }
                    catch
                    {
                    }

                    Thread.Sleep(1000);
                }

                PollingServer = false;
            });
        }

        #endregion
    }
}
