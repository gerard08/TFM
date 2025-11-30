using DetectorVulnerabilitats.Models;
using Microsoft.AspNetCore.Mvc;

namespace DetectorVulnerabilitats.Controllers
{
    [ApiController]
    public class ScansController : ControllerBase
    {
        private readonly ILogger<ScansController> _logger;
        private readonly IQueueService _queue;


        public ScansController(
            ILogger<ScansController> logger,
            IQueueService queue)
        {
            _logger = logger;
            _queue = queue;
        }

        [HttpGet("try")]
        public string StartScan()
        {
            return "okay";
        }

        [HttpPost("requestscan")]
        public async Task<IActionResult> StartScanAsync([FromBody] ScanRequest request)
        {
            Console.WriteLine($"SCAN REQUESTED WITH TARGET {request.Target} AND SCAN TYPE {request.ScanType}");
            _logger.LogInformation($"SCAN REQUESTED WITH TARGET {request.Target} AND SCAN TYPE {request.ScanType}");
            await _queue.EnqueueScanAsync(request);
            return Ok(new { status = "queued", target = request.Target });
        }
    }
}
