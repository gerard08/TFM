using DetectorVulnerabilitats.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class QueueService: IQueueService
{
    private const string QUEUE_SCAN_REQUEST_NAME = "scan-request";

    private readonly string _host;
    private IConnection _connection;
    private IChannel _channel;

    public QueueService(IConfiguration config)
    {
        _host = config["RabbitMQ:Host"] ?? "localhost";
        _channel = null!;
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
    }

    public async Task EnqueueScanAsync(ScanRequest request)
    {
        if(_channel is null)
        {
            await InitAsync();
        }

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: QUEUE_SCAN_REQUEST_NAME, body: body);
    }
}
