using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CalendarStorage.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ExporterController : Controller
    {
        private readonly ILogger<CalendarController> Logger;

        private readonly string ArchivePath;
        private readonly string VersionPath;
        private bool Enabled => this.ArchivePath != null && this.VersionPath != null;

        public ExporterController(ILogger<CalendarController> logger)
        {
            this.Logger = logger;
            this.ArchivePath = EnvConfig.ExporterArchivePath;
            this.VersionPath = EnvConfig.ExporterVersionPath;
        }

        [HttpGet]
        public IActionResult GetVersion()
        {
            if (!this.Enabled)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(this.VersionPath))
            {
                return NotFound();
            }

            return Ok(System.IO.File.ReadAllText(this.VersionPath));
        }

        [HttpGet]
        public IActionResult GetArchive()
        {
            if (!this.Enabled)
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(this.ArchivePath))
            {
                return NotFound();
            }

            var fs = new FileStream(this.ArchivePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.Response.RegisterForDispose(fs);
            return File(fs, "application/octet-stream", Path.GetFileName(this.ArchivePath));
        }
    }
}