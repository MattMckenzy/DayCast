using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace DayCastServer.Controllers
{
    public class QueueController : ControllerBase
    {
        [HttpGet("dequeue")]
        public IActionResult DequeueFiles()
        {
            List<string> returnFiles = new List<string>();

            while (Globals.QueuedFiles.TryDequeue(out FileInfo currentFile))
                if (currentFile != null)
                    returnFiles.Add(currentFile.FullName);

            return new JsonResult(returnFiles);
        }

        [HttpGet("enqueue/{*path}")]
        public IActionResult EnqueueFiles(string path)
        {
            return EnqueueFiles(path, DateTime.MinValue);
        }

        [HttpGet("enqueue/{minimumDateTime:datetime}/{*path}")]
        public IActionResult EnqueueFiles(string path, DateTime minimumDateTime)
        {
            DirectoryInfo directory = new DirectoryInfo(Uri.UnescapeDataString(path));
            FileInfo file = new FileInfo(Uri.UnescapeDataString(path));

            if (directory.Exists)
            {
                foreach (FileInfo currentFile in directory
                    .EnumerateFileSystemInfos("*.mp4", SearchOption.AllDirectories)
                    .Where(f => f.LastWriteTime > minimumDateTime)
                    .OrderBy(f => f.LastWriteTime))
                {
                    Globals.QueuedFiles.Enqueue(currentFile);
                }

                return new OkResult();
            }
            else if (file.Exists &&
                     file.Extension == ".mp4" &&
                     file.LastWriteTime > minimumDateTime)
            {
                Globals.QueuedFiles.Enqueue(file);

                return new OkResult();
            }
            else
                return new NotFoundResult();
        }
    }
}