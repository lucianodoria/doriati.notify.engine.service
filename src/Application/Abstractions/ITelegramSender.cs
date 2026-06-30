namespace Doriati.Notify.Engine.Application.Abstractions;

public interface ITelegramSender
{
    Task SendAsync(
        string botToken,
        string chatId,
        string message,
        string? parseMode,
        CancellationToken cancellationToken);
}
