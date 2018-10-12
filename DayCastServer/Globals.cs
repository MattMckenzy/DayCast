using System.Collections.Concurrent;
using System.IO;

namespace DayCastServer
{
    public static class Globals
    {
        public static ConcurrentQueue<FileInfo> QueuedFiles = new ConcurrentQueue<FileInfo>();
    }
}
