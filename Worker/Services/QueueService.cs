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
                    
                    var options = new ParallelOptions { MaxDegreeOfParallelism = 5 }; // Ajusta segons la RAM/CPU del teu servidor Ollama

                    var descriptionByAi = scanRequest.ScanType > ScanTypeEnum.Services; 
                    await Parallel.ForEachAsync(findings, options, async (finding, token) =>
                    {
                        // Important: Crear un scope per fil si FindSolutionWithAi usa serveis Scoped
                        // Com que la teva funció 'FindSolutionWithAi' ja crea el seu propi scope, pots cridar-la directament
                        finding.Solution = await FindSolutionWithAi(finding);
                        if (descriptionByAi && finding.Description != string.Empty)
                        {
                            finding.Description = await WriteDescriptionWithAi(finding);
                        }
                    });

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

    private async Task<string> WriteDescriptionWithAi(Findings finding)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LocalAiService>();
            return await context.GenerateDescriptionAsync(finding.Description);
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
