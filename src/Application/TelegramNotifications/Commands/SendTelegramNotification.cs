using Doriati.Notify.Engine.Application.Abstractions;
using Doriati.Notify.Engine.Application.TelegramNotifications.Dtos;

namespace Doriati.Notify.Engine.Application.TelegramNotifications.Commands;

public static class SendTelegramNotification
{
    public static Task HandleAsync(
        TelegramNotificationRequest request,
        ITelegramSender telegramSender,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BotToken))
            throw new InvalidOperationException("BotToken is required.");

        if (string.IsNullOrWhiteSpace(request.ChatId))
            throw new InvalidOperationException("ChatId is required.");

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new InvalidOperationException("Message is required.");

        return telegramSender.SendAsync(
            botToken: request.BotToken,
            chatId: request.ChatId,
            message: request.Message,
            parseMode: request.ParseMode,
            cancellationToken: cancellationToken);
    }
}
