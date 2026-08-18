using System.Text;
using Harness.BuildingBlocks.Infrastructure.Events;
using Harness.Modules.Integration.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Harness.Modules.Integration.Infrastructure;

/// <summary>
/// Consumer ERP/DMS: subscribe exchange fanout <c>harness.events</c> (Outbox Pattern đã publish),
/// map từng integration event sang bảng ERP qua <see cref="ErpSyncProcessor"/> rồi ACK.
/// Kết nối RabbitMQ lỗi sẽ tự retry mỗi 15s (không làm crash app khi broker chưa sẵn sàng).
/// </summary>
public class ErpSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _rabbit;
    private readonly ErpOptions _erp;
    private readonly ILogger<ErpSyncHostedService> _logger;

    public ErpSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> rabbit,
        IOptions<ErpOptions> erp,
        ILogger<ErpSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _rabbit = rabbit.Value;
        _erp = erp.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_erp.Enabled)
        {
            _logger.LogInformation("ERP consumer bị tắt (Integration:Erp:Enabled=false).");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ERP consumer chưa kết nối được RabbitMQ ({Error}) — thử lại sau 15s.", ex.Message);
                try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbit.HostName,
            Port = _rabbit.Port,
            UserName = _rabbit.UserName,
            Password = _rabbit.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(_rabbit.ExchangeName, ExchangeType.Fanout, durable: true);

        var queue = channel.QueueDeclare(_erp.QueueName, durable: true, exclusive: false, autoDelete: false).QueueName;
        channel.QueueBind(queue, _rabbit.ExchangeName, routingKey: "");
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, ea) =>
        {
            try
            {
                var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
                var eventType = ea.BasicProperties?.Type ?? "unknown";
                var eventId = Guid.TryParse(ea.BasicProperties?.MessageId, out var id) ? id : Guid.NewGuid();

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ErpSyncProcessor>();
                processor.ProcessAsync(eventType, eventId, payload).GetAwaiter().GetResult();

                channel.BasicAck(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("ERP đã consume {EventType} (event {EventId})", eventType, eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý message ERP, sẽ requeue.");
                channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        channel.BasicConsume(queue, autoAck: false, consumer);
        _logger.LogInformation("ERP consumer đang lắng nghe exchange '{Exchange}' (queue '{Queue}')", _rabbit.ExchangeName, queue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}