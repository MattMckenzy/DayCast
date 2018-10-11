using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Net.Sockets;

namespace DayCastServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder($"http://{args[0]}:{args[1]}").Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string hostAddress)
        {
            return WebHost.CreateDefaultBuilder()
            .UseKestrel()
            .UseUrls(hostAddress)
            .UseStartup<Startup>();
        }
    }
}
