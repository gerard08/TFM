using Worker.Models;
using Worker.Operations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Worker.Helpers;

public class QueueService: IQueueService
{
    private readonly string _host;
    private IConnection _connection;
    private IChannel _channel;
    private AsyncEventingBasicConsumer _consumer;

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
                    var result = await ScanOperations.RunScanAsync(scanRequest, cancellationToken);

                    Console.WriteLine($"Result {result}");
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
}
