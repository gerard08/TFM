using DetectorVulnerabilitats.Models;
public interface IQueueService
{
    Task EnqueueScanAsync(ScanRequest request);

}
