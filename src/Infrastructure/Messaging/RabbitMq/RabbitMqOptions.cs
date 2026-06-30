namespace Doriati.Notify.Engine.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = default!;
    public string UserName { get; init; } = default!;
    public string Password { get; init; } = default!;
    public int Port { get; init; }
    public string VirtualHost { get; init; } = default!;
    public string Exchange { get; set; } = default!;
    public string ConsumeQueue { get; init; } = "notifications.telegram";
}
