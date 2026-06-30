using Doriati.Notify.Engine.Infrastructure.Messaging.RabbitMq;

namespace Doriati.Notify.Engine.Api.Services;

public sealed class RabbitMqTelegramHostedService : BackgroundService
{
    private readonly ILogger<RabbitMqTelegramHostedService> _logger;
    private readonly RabbitMqTelegramConsumer _consumer;

    public RabbitMqTelegramHostedService(
        ILogger<RabbitMqTelegramHostedService> logger,
        RabbitMqTelegramConsumer consumer)
    {
        _logger = logger;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Telegram consumer hosted service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _consumer.StartAsync(stoppingToken);
                _logger.LogInformation("RabbitMQ Telegram consumer is consuming.");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ Telegram consumer failed to start. Retrying...");

                try
                {
                    await _consumer.StopAsync(stoppingToken);
                }
                catch (Exception stopEx)
                {
                    _logger.LogError(stopEx, "Failed to stop RabbitMQ consumer after an error.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consumer.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
