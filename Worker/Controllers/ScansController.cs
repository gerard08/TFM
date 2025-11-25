using Worker.Models;
using Microsoft.AspNetCore.Mvc;

namespace DetectorVulnerabilitats.Controllers
{
    [ApiController]
    public class ScansController : ControllerBase
    {
        private readonly ILogger<ScansController> _logger;


        public ScansController(
            ILogger<ScansController> logger)
        {
            _logger = logger;
        }

        [HttpGet("try")]
        public string StartScan()
        {
            return "HttpStatusCode";
        }
    }
}
