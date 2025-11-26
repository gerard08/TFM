using DetectorVulnerabilitatsDatabase.Context;
using DetectorVulnerabilitatsDatabase.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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

                

                Console.WriteLine(message);
                try
                {
                    // Scan
                    //var findings = await ScanOperations.RunScanAsync(scanRequest, cancellationToken);
                    var findings = new List<Findings> { 
                        new(){
                            Title = "CVE-2024-24747",
                            Severity = "8.8",
                            Cve_id = "CVE-2024-24747",
                            Affected_service = "MinIO ",
                            Description = "MinIO is a High Performance Object Storage. When someone creates an access key, it inherits the permissions of the parent key. Not only for `s3:*` actions, but also `admin:*` actions. Which means unless somewhere above in the access-key hierarchy, the `admin` rights are denied, access keys will be able to simply override their own `s3` permissions to something more permissive. The vulnerability is fixed in RELEASE.2024-01-31T20-20-33Z.",
                            Created_at = DateTime.UtcNow
                        } 
                    };
                    
                    // Look for solutions
                    foreach(var finding in findings)
                    {
                        finding.Solution = await FindSolutionWithAi(finding);
                    }

                    // Save finding

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        await _channel.BasicConsumeAsync("scans", autoAck: false, consumer: _consumer);
    }

    private async Task AddDataToDbAsync<T>(T data) where T : class
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DbOperations>();
            await context.AddObjectToDbAsync(data);
        }
    }

    private async Task<List<T>> LlegirTaulaSenceraAsync<T>() where T : class
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DbOperations>();
            return await context.GetAllAsync<T>();
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
