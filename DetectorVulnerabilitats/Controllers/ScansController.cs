using DetectorVulnerabilitats.Models;
using DetectorVulnerabilitats.Services;
using Microsoft.AspNetCore.Mvc;

namespace DetectorVulnerabilitats.Controllers
{
    [ApiController]
    public class ScansController : ControllerBase
    {
        private readonly ILogger<ScansController> _logger;
        private readonly IQueueService _queue;
        private readonly ResultsReaderService _resultsReaderService;


        public ScansController(
            ILogger<ScansController> logger,
            IQueueService queue,
            ResultsReaderService resultsReaderService)
        {
            _logger = logger;
            _queue = queue;
            _resultsReaderService = resultsReaderService;
        }

        [HttpPost("requestscan")]
        public async Task<IActionResult> StartScanAsync([FromBody] ScanRequest request)
        {
            Console.WriteLine($"SCAN REQUESTED WITH TARGET {request.Target} AND SCAN TYPE {request.ScanType}");
            _logger.LogInformation($"SCAN REQUESTED WITH TARGET {request.Target} AND SCAN TYPE {request.ScanType}");
            await _queue.EnqueueScanAsync(request);
            return Ok(new { status = "queued", target = request.Target });
        }

        [HttpGet("allscanresults")]
        public async Task<IActionResult> GetScanResultsAsync()
        {
            Console.WriteLine($"RESULTS REQUESTED");
            _logger.LogInformation($"RESULTS REQUESTED");
            var scanResults = await _resultsReaderService.GetAllScanResultsAsync();
            return Ok(scanResults);
        }

        [HttpGet("scanresult/{id}")]
        public async Task<IActionResult> GetScanResultsAsync(Guid id)
        {
            Console.WriteLine($"RESULTS REQUESTED");
            _logger.LogInformation($"RESULTS REQUESTED");
            var scanResults = await _resultsReaderService.GetScanResultById(id);

            if(scanResults is not null)
            {
                return Ok(scanResults);
            }
            return NotFound($"Scan with request id {id} was not found in the server.");

        }
    }
}
