using DetectorVulnerabilitatsDatabase.Context;
using DetectorVulnerabilitatsDatabase.Models;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Worker.Helpers;
using Worker.Models;
using Worker.Operations;
using Worker.Services;

public class QueueService: IQueueService
{
    private readonly string _host;
    private IConnection _connection;
    private IChannel _channel;
    private AsyncEventingBasicConsumer _consumer;
    private IServiceScopeFactory _scopeFactory;

    public QueueService(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _host = config["RabbitMQ:Host"] ?? "localhost";
        _channel = null!;
        _scopeFactory = scopeFactory;
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

        await _channel.QueueDeclareAsync(queue: "scans", durable: true, exclusive: false, autoDelete: false, arguments: null);

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

            if(scanRequest is not null)
            {
                var scanTaskGuid = await SaveScanTaskToDb(scanRequest);

                try
                {
                    // Scan
                    var findings = await ScanOperations.RunScanAsync(scanRequest, cancellationToken);
                    //var findings = new List<Findings> { 
                    //    new(){
                    //        Title = "CVE-2024-24747",
                    //        Severity = "8.8",
                    //        Cve_id = "CVE-2024-24747",
                    //        Affected_service = "MinIO ",
                    //        Description = "MinIO is a High Performance Object Storage. When someone creates an access key, it inherits the permissions of the parent key. Not only for `s3:*` actions, but also `admin:*` actions. Which means unless somewhere above in the access-key hierarchy, the `admin` rights are denied, access keys will be able to simply override their own `s3` permissions to something more permissive. The vulnerability is fixed in RELEASE.2024-01-31T20-20-33Z.",
                    //        Created_at = DateTime.UtcNow
                    //    } 
                    //};

                    foreach (var finding in findings)
                    {
                        finding.Solution = await FindSolutionWithAi(finding);
                    }

                    await SaveResultsToDb(scanRequest, scanTaskGuid, findings);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    await MarkTaskAsFailed(scanTaskGuid);
                }
            }

            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        await _channel.BasicConsumeAsync("scans", autoAck: false, consumer: _consumer);
    }

    private async Task<Guid> SaveScanTaskToDb(ScanRequest scanRequest)
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

    private async Task SaveResultsToDb(ScanRequest scanRequest, Guid scanTaskGuid, List<Findings> findings)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DetectorVulnerabilitatsDatabaseContext>();
            

                var taskStub = new ScanTask { Id = scanTaskGuid };
                context.ScanTasks.Attach(taskStub);

                taskStub.Status = "finished";
                taskStub.Finished_at = DateTime.UtcNow;

                // Look for solutions
                var scanResults = new ScanResults()
                {
                    Id = Guid.NewGuid(),
                    Scan_task_id = scanTaskGuid,
                    Summary = "not implemented yet",
                    Created_at = DateTime.UtcNow,
                    Findings = findings
                };
            
                context.ScanResults.Add(scanResults);
                await context.SaveChangesAsync();
        }
    }

    private async Task MarkTaskAsFailed(Guid taskId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DetectorVulnerabilitatsDatabaseContext>();

            // Fem servir el mateix truc del Stub/Attach per ser ràpids
            var taskStub = new ScanTask { Id = taskId };
            context.ScanTasks.Attach(taskStub);

            taskStub.Status = "failed";
            taskStub.Finished_at = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
    }

    private async Task<string> FindSolutionWithAi(Findings finding)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LocalAiService>();
            return await context.GenerateFixAsync(finding.Cve_id, finding.Description, finding.Affected_service);
        }
    }
}
