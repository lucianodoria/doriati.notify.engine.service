using Doriati.Notify.Engine.Application.Abstractions;
using Doriati.Notify.Engine.Application.TelegramNotifications.Commands;
using Doriati.Notify.Engine.Application.TelegramNotifications.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Doriati.Notify.Engine.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqTelegramConsumer
{
    private IConnection? _connection;
    private IChannel? _channel;

    private readonly ILogger<RabbitMqTelegramConsumer> _logger;
    private readonly ITelegramSender _telegramSender;
    private readonly RabbitMqOptions _options;

    public RabbitMqTelegramConsumer(
        ILogger<RabbitMqTelegramConsumer> logger,
        ITelegramSender telegramSender,
        IOptions<RabbitMqOptions> options)
    {
        _logger = logger;
        _telegramSender = telegramSender;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclarePassiveAsync(queue: _options.ConsumeQueue);

        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var request = JsonSerializer.Deserialize<TelegramNotificationRequest>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (request is null)
                    throw new InvalidOperationException("Invalid payload.");

                await SendTelegramNotification.HandleAsync(request, _telegramSender, stoppingToken);

                _logger.LogInformation(
                    "Telegram notification sent successfully (RabbitMQ). Queue: {Queue} ChatId: {ChatId}",
                    _options.ConsumeQueue,
                    request.ChatId);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process RabbitMQ Telegram notification. Queue: {Queue} DeliveryTag: {DeliveryTag}",
                    _options.ConsumeQueue,
                    ea.DeliveryTag);

                await _channel.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _options.ConsumeQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
            await _channel.CloseAsync(cancellationToken);

        if (_connection != null)
            await _connection.CloseAsync(cancellationToken);
    }
}
