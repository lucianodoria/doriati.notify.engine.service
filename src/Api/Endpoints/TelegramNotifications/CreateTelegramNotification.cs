using Doriati.Notify.Engine.Application.Abstractions;
using Doriati.Notify.Engine.Application.TelegramNotifications.Commands;
using Doriati.Notify.Engine.Application.TelegramNotifications.Dtos;

namespace Doriati.Notify.Engine.Api.Endpoints.TelegramNotifications;

public sealed class CreateTelegramNotificationHandler
{
    public static async Task<IResult> HandleAsync(
        TelegramNotificationRequest request,
        ITelegramSender telegramSender,
        ILogger<CreateTelegramNotificationHandler> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await SendTelegramNotification.HandleAsync(request, telegramSender, cancellationToken);
            logger.LogInformation("Telegram notification sent successfully via HTTP. ChatId: {ChatId}", request.ChatId);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Telegram notification via HTTP.");
            return Results.Problem("An error occurred while sending the notification.");
        }
    }
}
