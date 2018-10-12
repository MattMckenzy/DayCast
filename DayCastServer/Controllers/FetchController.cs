using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace DayCastServer.Controllers
{
    public class FetchController : ControllerBase
    {
        private readonly IConfiguration _Configuration;

        public FetchController(IConfiguration configuration) => _Configuration = configuration;

        [HttpGet("fetch/{*path}")]
        public IActionResult FetchFile(string path)
        {
            FileInfo file = new FileInfo(System.Uri.UnescapeDataString(path));

            if (file.Exists)
            {
                string contentType = string.Empty;
                switch (file.Extension)
                {
                    case ".mp4":
                        contentType = "video/mp4"; break;
                }

                if (!string.IsNullOrWhiteSpace(contentType))
                    return File(file.Open(FileMode.Open, FileAccess.Read, FileShare.Read), contentType, true);
                else
                    return new UnsupportedMediaTypeResult();
            }
            else
                return new NotFoundResult();
        }
    }
}
