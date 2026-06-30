namespace Doriati.Notify.Engine.Application.TelegramNotifications.Dtos;

public sealed record TelegramNotificationRequest
{
    public required string BotToken { get; init; }
    public required string ChatId { get; init; }
    public required string Message { get; init; }
    public string ParseMode { get; init; } = "MarkdownV2";
}
