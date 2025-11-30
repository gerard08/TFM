using DetectorVulnerabilitatsDatabase.Models;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DetectorVulnerabilitats.Services;

public class ScanUpdateListener : BackgroundService
{
    private readonly IHubContext<ScanHub> _hubContext;
    private readonly string _hostname;
    private const string QUEUE_SCAN_RESULT_NAME = "scan-result";
    private IConnection _connection;
    private IChannel _channel;

    public ScanUpdateListener(IHubContext<ScanHub> hubContext, IConfiguration config)
    {
        _hubContext = hubContext;
        _hostname = config["RabbitMQ:Host"] ?? "localhost";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Configuració de connexió (Igual que el teu QueueService)
        var factory = new ConnectionFactory
        {
            HostName = _hostname,
            UserName = "guest",
            Password = "guest"
        };

        // Connectem
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync();

        // 2. Declarem la cua per assegurar que existeix
        await _channel.QueueDeclareAsync(queue: QUEUE_SCAN_RESULT_NAME, durable: true, exclusive: false, autoDelete: false);

        // 3. Configurem el consumidor
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // Deserialitzem el missatge petit que ve del Worker
                // Assegura't de tenir la classe ScanCompletedEvent al projecte API també
                var scanEvent = JsonSerializer.Deserialize<string>(message);

                if (scanEvent != null)
                {
                    // 4. AQUÍ ESTÀ LA MÀGIA: Enviem a l'Angular via SignalR
                    await _hubContext.Clients.All.SendAsync("ScanFinished", scanEvent, stoppingToken);

                    Console.WriteLine($"[API] Notificació enviada");
                }

                // Confirmem missatge processat
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processant resposta del worker: {ex.Message}");
            }
        };

        // Comencem a escoltar
        await _channel.BasicConsumeAsync(queue: QUEUE_SCAN_RESULT_NAME, autoAck: false, consumer: consumer);

        // Mantenim el fil viu
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}