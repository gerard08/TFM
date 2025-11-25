using Worker.Models;
public interface IQueueService
{
    Task ReceiveScanAsync(CancellationToken cancellationToken);

}
