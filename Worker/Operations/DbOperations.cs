using DetectorVulnerabilitatsDatabase.Context;
using DetectorVulnerabilitatsDatabase.Models;
using Microsoft.EntityFrameworkCore;
using Worker.Models;

namespace Worker.Operations
{
    public class DbOperations
    {
        private readonly DetectorVulnerabilitatsDatabaseContext _context;
        private IServiceScopeFactory _scopeFactory;

        public DbOperations(DetectorVulnerabilitatsDatabaseContext context, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
        }

        public async Task AddObjectToDbAsync<T>(T obj) where T : class
        {
            await _context.Set<T>().AddAsync(obj);
            await _context.SaveChangesAsync();
        }

        public async Task<List<T>> GetAllAsync<T>() where T : class
        {
            return await _context.Set<T>()
                                 .ToListAsync();
        }

        public async Task<Guid> SaveScanTaskToDb(ScanRequest scanRequest)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DetectorVulnerabilitatsDatabaseContext>();
                var asset = await context.Assets.FirstOrDefaultAsync(x => x.Ip == scanRequest.Target);
                if (asset == null)
                {
                    asset = new Assets()
                    {
                        Id = Guid.NewGuid(),
                        Ip = scanRequest.Target,
                        First_scanned_at = DateTime.UtcNow,
                        Last_scanned_at = DateTime.UtcNow
                    };
                    context.Assets.Add(asset);
                }
                else
                {
                    asset.Last_scanned_at = DateTime.UtcNow;
                }

                var scanTask = new ScanTask()
                {
                    Id = Guid.NewGuid(),
                    Asset = asset,
                    Scan_type = scanRequest.ScanType.ToString(),
                    Status = "running",
                    Requested_by = "not impemented yet",
                    Created_at = scanRequest.CreationTime,
                    Started_at = DateTime.UtcNow
                };
                context.ScanTasks.Add(scanTask);

                await context.SaveChangesAsync();

                return scanTask.Id;
            }
        }

        public async Task<ScanResults> SaveResultsToDb(ScanRequest scanRequest, Guid scanTaskGuid, List<Findings> findings)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DetectorVulnerabilitatsDatabaseContext>();


                var taskStub = new ScanTask { Id = scanTaskGuid };
                context.ScanTasks.Attach(taskStub);

                taskStub.Status = "completed";
                taskStub.Finished_at = DateTime.UtcNow;

                // Look for solutions
                var scanResults = new ScanResults()
                {
                    Id = Guid.NewGuid(),
                    Scan_task_id = scanTaskGuid,
                    Summary = $"Scanned {scanRequest.ScanType} on IP {scanRequest.Target} and found {findings.Count} findings.",
                    Created_at = DateTime.UtcNow,
                    Findings = findings
                };

                context.ScanResults.Add(scanResults);
                await context.SaveChangesAsync();
                return scanResults;
            }
        }
    }
}
