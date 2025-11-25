namespace Worker.Services
{
    public class QueueWorker : BackgroundService
    {
        private readonly IQueueService _queueService;

        public QueueWorker(IQueueService queueService)
        {
            _queueService = queueService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _queueService.ReceiveScanAsync(stoppingToken);
        }
    }

}
