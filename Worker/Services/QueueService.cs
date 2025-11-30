using DetectorVulnerabilitatsDatabase.Context;
using DetectorVulnerabilitatsDatabase.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Worker.Models;
using Worker.Operations;
using Worker.Services;

public class QueueService: IQueueService
{
    private const string QUEUE_SCAN_REQUEST_NAME = "scan-request";
    private const string QUEUE_SCAN_RESULT_NAME = "scan-result";

    private readonly string _host;
    private IConnection _connection;
    private IChannel _channel;
    private AsyncEventingBasicConsumer _consumer;
    private IServiceScopeFactory _scopeFactory;
    private LocalAiService _localAiService;

    public QueueService(IConfiguration config,
        IServiceScopeFactory scopeFactory,
        LocalAiService localAiService)
    {
        _host = config["RabbitMQ:Host"] ?? "localhost";
        _channel = null!;
        _scopeFactory = scopeFactory;
        _localAiService = localAiService;
    }

    private async Task InitAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _host,
            UserName = "guest",
            Password = "guest"
        };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.QueueDeclareAsync(queue: QUEUE_SCAN_REQUEST_NAME, durable: true, exclusive: false, autoDelete: false, arguments: null);
        await _channel.QueueDeclareAsync(queue: QUEUE_SCAN_RESULT_NAME, durable: true, exclusive: false, autoDelete: false, arguments: null);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
        _consumer = new AsyncEventingBasicConsumer(_channel);
        Console.WriteLine("CONNECTED TO THE QUEUE");
    }

    public async Task ReceiveScanAsync(CancellationToken cancellationToken)
    {
        if(_channel is null)
        {
            await InitAsync();
        }

        _consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var scanRequest = JsonSerializer.Deserialize<ScanRequest>(message);

            await PerformScan(scanRequest, cancellationToken);

            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        await _channel.BasicConsumeAsync(QUEUE_SCAN_REQUEST_NAME, autoAck: false, consumer: _consumer);
    }

    private async Task PublishScanFinishedAsync()
    {
       var json = JsonSerializer.Serialize("success");
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties(); // En versions noves potser has de fer servir _channel.CreateBasicProperties()

        // Enviem el missatge a l'Exchange per defecte ("") amb routingKey = nom de la cua
        await _channel.BasicPublishAsync(exchange: "", routingKey: QUEUE_SCAN_RESULT_NAME, mandatory: false, basicProperties: props, body: body);
    }

    private async Task PerformScan(ScanRequest scanRequest, CancellationToken cancellationToken)
    {
        if (scanRequest is not null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbOperations = scope.ServiceProvider.GetRequiredService<DbOperations>();
                var scanTaskGuid = await dbOperations.SaveScanTaskToDb(scanRequest);

                try
                {
                    var findings = await ScanOperations.RunScanAsync(scanRequest, cancellationToken);

                    var options = new ParallelOptions { MaxDegreeOfParallelism = 5 };

                    var descriptionByAi = scanRequest.ScanType > ScanTypeEnum.Services;
                    await Parallel.ForEachAsync(findings, options, async (finding, token) =>
                    {
                        finding.Solution = await _localAiService.FindSolutionWithAi(finding);
                        if (descriptionByAi && finding.Description != string.Empty)
                        {
                            finding.Description = await _localAiService.WriteDescriptionWithAi(finding);
                        }
                    });
                    var scanResults = await dbOperations.SaveResultsToDb(scanRequest, scanTaskGuid, findings);
                    Console.WriteLine("ENTER");
                    await Task.Delay(5000);
                    Console.WriteLine("SENDING");
                    await PublishScanFinishedAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    await MarkTaskAsFailed(scanTaskGuid);

                    await PublishScanFinishedAsync();
                }
            }
        }
    }

    private async Task MarkTaskAsFailed(Guid taskId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DetectorVulnerabilitatsDatabaseContext>();

            var taskStub = new ScanTask { Id = taskId };
            context.ScanTasks.Attach(taskStub);

            taskStub.Status = "failed";
            taskStub.Finished_at = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
    }
}
