using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;

namespace DayCast
{
    class Program
    {
        enum Options
        {
            Unassigned,
            IPAddress,
            CutoffTime,
            Help
        }

        static void Main(string[] Arguments)
        {
            // Argument Validation
            IPAddress castHost = IPAddress.Any;
            DateTime cutoffDateTime = DateTime.MinValue;
            List<FileInfo> filesToPlay = new List<FileInfo>();

            Options nextArgument = Options.Unassigned;
            foreach (string argument in Arguments)
            {
                switch (nextArgument)
                {
                    case Options.IPAddress:
                        castHost = new IPAddress(argument.Split('.').Select(b => Byte.Parse(b)).ToArray());
                        break;

                    case Options.CutoffTime:
                        if (argument.ToUpper() == "TODAY")
                            cutoffDateTime = DateTime.Now.Date;
                        else
                            cutoffDateTime = DateTime.Parse(argument);
                        break;

                    case Options.Help:
                        Console.WriteLine("Available options:");
                        Console.WriteLine("-a, --address: IP address of the network device on which to cast.");
                        Console.WriteLine("-t, --time: Cutoff time for which the last write time of the video files needs to surpass. (\"today\" can be used to limit to today's videos)");
                        Console.WriteLine("-h, -?, ?, --help: Help text currently being read.");
                        Console.WriteLine("The files and directories from which to find and cast *.mp4 files can be listed sequentially as any argument.");
                        return;

                    case Options.Unassigned:
                        if (Directory.Exists(argument))
                        {
                            DirectoryInfo currentDirectory = new DirectoryInfo(argument);
                            foreach (FileInfo file in currentDirectory.EnumerateFileSystemInfos("*.mp4", SearchOption.AllDirectories))
                                filesToPlay.Add(file);
                        }
                        else if (File.Exists(argument))
                        {
                            filesToPlay.Add(new FileInfo(argument));
                        }
                        break;
                }

                nextArgument = Options.Unassigned;
                if (new string[] { "-a", "--address" }.Contains(argument))
                    nextArgument = Options.IPAddress;
                if (new string[] { "-t", "--time" }.Contains(argument))
                    nextArgument = Options.CutoffTime;
                if (new string[] { "-h", "-?", "?", "--help" }.Contains(argument))
                    nextArgument = Options.Help;
            }

            if (filesToPlay.Count == 0)
                throw new Exception("No videos found! Please include the full path of the directory or files to play.");

            string command = $"castnow " +
                $"{(castHost != IPAddress.Any ? $"--address {castHost.ToString()}" : string.Empty)} " +
                $"{string.Join(" ", filesToPlay.Where(f => f.LastWriteTime > cutoffDateTime).OrderBy(f => f.LastWriteTime).Select(f => $"\"{f.FullName}\""))}";

            NamedPipeServerStream stream = new NamedPipeServerStream("DayCastPipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);            
        }
    }
}