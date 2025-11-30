using DetectorVulnerabilitatsDatabase.Context;
using DetectorVulnerabilitatsDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Worker.Models;

namespace DetectorVulnerabilitats.Services
{
    public class ResultsReaderService
    {
        DetectorVulnerabilitatsDatabaseContext _dbcontext = null!;
        public ResultsReaderService(DetectorVulnerabilitatsDatabaseContext dbContext)
        {
            _dbcontext = dbContext;
        }

        public async Task<List<ScanResponse>> GetAllScanResultsAsync()
        {
            var result = new List<ScanResponse>();

            var FailedScanTasks = _dbcontext.ScanTasks
                .Where(x => string.Equals(x.Status, "failed") || string.Equals(x.Status, "running"))
                .Include(x => x.Asset)
                .ToList();

            foreach (var failedTask in FailedScanTasks) {
                result.Add(CreateCustomScanResult(failedTask, failedTask.Status.ToUpper()));
            }

            var scanResults = await _dbcontext.ScanResults
                    .Include(x => x.ScanTask)
                        .ThenInclude(t => t.Asset)
                    .Include(x => x.Findings)
                    .Where(x => x.ScanTask != null)
                    .ToListAsync();
            
            foreach (var scanResult in scanResults)
            {
                if (!string.Equals(scanResult.ScanTask.Status, "running"))
                {
                    result.Add(CreateScanResult(scanResult));
                }
                else
                {
                    result.Add(CreateCustomScanResult(scanResult.ScanTask, "RUNNING"));
                }
            }

            return result.OrderByDescending(x=> x.Date).Distinct().ToList();
        }

        private ScanResponse CreateCustomScanResult(ScanTask scanTask, string state)
        {
            return new ScanResponse()
            {
                State = state,
                ScanType = scanTask.Scan_type,
                Target = scanTask.Asset.Ip,
                Date = scanTask.Created_at.ToString("dd/MM/yyyy HH:mm:ss"),
                VulnerabilityCount = "0",
                Duration = "0"
            };
        }

        private ScanResponse CreateScanResult(ScanResults scanResult)
        {
            string resultStat = "SAFE";

            if (scanResult.Findings is not null && scanResult.Findings.Any())
            {
                resultStat = scanResult.Findings.Any(x => float.Parse(x.Severity) > 5.0) ? "CRITICAL" : "WARNING";
            }

                return new ScanResponse()
                {
                    State = resultStat,
                    ScanType = scanResult.ScanTask.Scan_type,
                    Target = scanResult.ScanTask.Asset.Ip,
                    Date = scanResult.Created_at.ToString("dd/MM/yyyy HH:mm:ss"),
                    VulnerabilityCount = scanResult.Findings.Count().ToString(),
                    Duration = (scanResult.ScanTask.Finished_at - scanResult.ScanTask.Started_at).ToString(@"hh\:mm\:ss")
                };
        }
    }
}
